using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Analytics.Api.Rules;
using Contracts.Training;

namespace Analytics.Api.Dashboard;

/// <summary>A dashboard összesítő időszaka: az aktuális hét (hétfőtől) vagy hónap.</summary>
public enum DashboardPeriod
{
    Week,
    Month,
}

/// <summary>Összesítő kártyák az időszakra (spec 3.5).</summary>
public record DashboardSummary(
    DateOnly From,
    DateOnly To,
    decimal DistanceKm,
    int DurationSeconds,
    int ElevationGainM,
    int WorkoutCount,
    int? AvgPaceSecondsPerKm,
    int? RiskScore,
    RiskCategory? RiskCategory);

/// <summary>Intenzitáseloszlás percben: könnyű (RPE 1–4), közepes (5–6), intenzív (7–10).</summary>
public record IntensityDistribution(int EasyMinutes, int ModerateMinutes, int IntenseMinutes);

/// <summary>
/// Egy hét adatai a diagramokhoz: heti km, heti terhelés, ACWR és kockázati pontszám a hét végén
/// (a folyó hétnél ma), valamint terv és tény összevetése.
/// </summary>
public record WeeklyStats(
    DateOnly WeekStart,
    decimal DistanceKm,
    double Load,
    double? Acwr,
    int RiskScore,
    int WorkoutCount,
    int PlannedCount,
    int CompletedCount,
    int MissedCount);

/// <summary>A terhelési dashboard (UC06): összesítő, intenzitáseloszlás és az utolsó 12 hét.</summary>
public record DashboardResponse(
    DashboardPeriod Period,
    DashboardSummary Summary,
    IntensityDistribution Intensity,
    List<WeeklyStats> Weeks);

/// <summary>
/// A dashboard számításai. I/O nélküli, ezért egységtesztekkel ellenőrizhető; a heti ACWR-t és
/// kockázati pontszámot ugyanaz a <see cref="RiskCalculator"/> adja, mint a napi pontszámot.
/// </summary>
public static class DashboardCalculator
{
    public const int WeekCount = 12;

    /// <summary>
    /// Ennyi napra visszamenő edzés kell: 12 hét, plusz a legkorábbi hét krónikus terheléséhez 27 nap.
    /// </summary>
    public const int RequiredHistoryDays = WeekCount * 7 + 27;

    /// <summary>
    /// A dashboard a mai napra.
    /// </summary>
    /// <param name="workouts">Az edzések legalább <see cref="RequiredHistoryDays"/> napra visszamenőleg.</param>
    /// <param name="plannedWorkouts">A tervezett edzések az utolsó 12 hétre (a folyó hét végéig).</param>
    /// <param name="firstWorkoutDate">A legelső edzés napja (az ACWR-hez kell, lásd <see cref="RiskCalculator"/>).</param>
    /// <param name="currentRisk">A legutóbbi tárolt kockázati pontszám, ha van.</param>
    /// <param name="settings">A kockázati szabályok beállításai; ha nincs megadva, a spec alapértelmezései.</param>
    public static DashboardResponse Calculate(
        DashboardPeriod period,
        IReadOnlyCollection<WorkoutRecord> workouts,
        IReadOnlyCollection<PlannedWorkoutRecord> plannedWorkouts,
        DateOnly today,
        DateOnly? firstWorkoutDate,
        RiskScoreSnapshot? currentRisk,
        RuleSettings? settings = null)
    {
        // Az időszak a hét (hétfőtől) vagy a hónap eleje és a mai nap közötti rész.
        var from = period == DashboardPeriod.Week ? WeekStart(today) : new DateOnly(today.Year, today.Month, 1);
        var inPeriod = workouts.Where(w => w.Date >= from && w.Date <= today).ToList();

        var distanceKm = inPeriod.Sum(w => w.DistanceKm);
        var durationSeconds = inPeriod.Sum(w => w.DurationSeconds);
        var summary = new DashboardSummary(
            from,
            today,
            distanceKm,
            durationSeconds,
            inPeriod.Sum(w => w.ElevationGainM ?? 0),
            inPeriod.Count,
            distanceKm > 0 ? (int)Math.Round(durationSeconds / distanceKm, MidpointRounding.AwayFromZero) : null,
            currentRisk?.Score,
            currentRisk?.Category);

        var intensity = new IntensityDistribution(
            Minutes(inPeriod, 1, 4),
            Minutes(inPeriod, 5, 6),
            Minutes(inPeriod, 7, 10));

        // Az utolsó 12 hét, a legrégebbi elöl; az utolsó a folyó hét.
        var currentWeekStart = WeekStart(today);
        var weeks = Enumerable.Range(0, WeekCount)
            .Select(index => currentWeekStart.AddDays(-7 * (WeekCount - 1 - index)))
            .Select(weekStart => Week(weekStart, workouts, plannedWorkouts, today, firstWorkoutDate, settings))
            .ToList();

        return new DashboardResponse(period, summary, intensity, weeks);
    }

    /// <summary>
    /// A nap hetének hétfője.
    /// </summary>
    public static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>
    /// Egy hét adatai. Az ACWR és a pontszám a hét utolsó napjára (a folyó hétnél a mai napra) számolódik.
    /// </summary>
    private static WeeklyStats Week(
        DateOnly weekStart,
        IReadOnlyCollection<WorkoutRecord> workouts,
        IReadOnlyCollection<PlannedWorkoutRecord> plannedWorkouts,
        DateOnly today,
        DateOnly? firstWorkoutDate,
        RuleSettings? settings)
    {
        var weekEnd = weekStart.AddDays(6);
        var evaluationDay = weekEnd < today ? weekEnd : today;

        var weekWorkouts = workouts.Where(w => w.Date >= weekStart && w.Date <= weekEnd).ToList();
        var weekPlanned = plannedWorkouts.Where(p => p.Date >= weekStart && p.Date <= weekEnd).ToList();

        // A kockázati számításhoz a kiértékelés napját megelőző 7 nap kihagyott edzései kellenek.
        var missedLast7Days = plannedWorkouts.Count(p =>
            p.Status == PlannedWorkoutStatus.Missed && p.Date >= evaluationDay.AddDays(-6) && p.Date <= evaluationDay);
        var risk = RiskCalculator.Calculate(workouts, missedLast7Days, evaluationDay, firstWorkoutDate, settings);

        return new WeeklyStats(
            weekStart,
            weekWorkouts.Sum(w => w.DistanceKm),
            weekWorkouts.Sum(w => w.Load),
            risk.Acwr,
            risk.Score,
            weekWorkouts.Count,
            weekPlanned.Count,
            weekPlanned.Count(p => p.Status == PlannedWorkoutStatus.Completed),
            weekPlanned.Count(p => p.Status == PlannedWorkoutStatus.Missed));
    }

    /// <summary>
    /// Az adott RPE-sávba eső edzések összideje percben.
    /// </summary>
    private static int Minutes(IEnumerable<WorkoutRecord> workouts, int minRpe, int maxRpe) =>
        workouts.Where(w => w.Rpe >= minRpe && w.Rpe <= maxRpe).Sum(w => w.DurationSeconds) / 60;
}
