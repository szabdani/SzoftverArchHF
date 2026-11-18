using Analytics.Api.ReadModels;
using Analytics.Api.Risk;
using Contracts;
using Contracts.Training;

namespace Analytics.Api.Recommendations;

/// <summary>
/// Ajánlott következő edzés: típus, cél RPE-tartomány, hozzávetőleges időtartam (és ha van, táv), rövid indoklással.
/// </summary>
public record Recommendation(
    WorkoutType Type,
    int RpeMin,
    int RpeMax,
    int DurationMinutes,
    decimal? DistanceKm,
    string Reason);

/// <summary>
/// Az edzésjavaslat szabályai (spec 3.7). A szabályok sorrendben értékelődnek, az első teljesülő dönt.
/// Determinisztikus, I/O nélküli, ezért egységtesztekkel ellenőrizhető.
/// </summary>
public static class RecommendationRules
{
    /// <summary>Közepes kockázatnál a tervezett edzés ennyiszeresére rövidül.</summary>
    public const decimal MediumRiskShortening = 0.8m;

    /// <summary>A félmaratoni és hosszabb célnál a minőségi edzés tempó, rövidebbnél intervall.</summary>
    public const decimal TempoGoalMinDistanceKm = 21.0975m;

    /// <summary>
    /// A javaslat a mai napra.
    /// </summary>
    /// <param name="riskCategory">A mai (vagy legutóbbi) kockázati kategória; null, ha még nincs adat.</param>
    /// <param name="recentWorkouts">A futó edzései legalább az aktuális hét elejéig és az utolsó két napra visszamenőleg.</param>
    /// <param name="plannedToday">A mai napra tervezett, még nem teljesített edzés, ha van.</param>
    /// <param name="goalType">Az aktív cél típusa, ha van.</param>
    /// <param name="goalDistanceKm">Az aktív cél távja, ha van.</param>
    /// <param name="today">A mai nap a felhasználó időzónájában.</param>
    public static Recommendation Recommend(
        RiskCategory? riskCategory,
        IReadOnlyCollection<WorkoutRecord> recentWorkouts,
        PlannedWorkoutRecord? plannedToday,
        GoalType? goalType,
        decimal? goalDistanceKm,
        DateOnly today)
    {
        // 1. Magas kockázat: pihenő vagy regeneráló futás.
        if (riskCategory == RiskCategory.High)
        {
            return new(WorkoutType.Recovery, 2, 3, 30, null,
                "Magas a kockázati pontszám: ma pihenőnap vagy legfeljebb egy rövid, regeneráló futás javasolt.");
        }

        // 2. Ha tegnap intenzív nap volt (RPE ≥ 7), ma könnyű futás.
        var yesterday = today.AddDays(-1);
        if (recentWorkouts.Any(w => w.Date == yesterday && w.Rpe >= RiskCalculator.IntenseRpe))
        {
            return new(WorkoutType.Easy, 3, 4, 40, null,
                "Tegnap intenzív edzés volt (RPE ≥ 7), ezért ma könnyű futás javasolt.");
        }

        // 3. A mai napra tervezett edzés; közepes kockázatnál eggyel alacsonyabb RPE-vel és rövidebben.
        if (plannedToday is { Status: PlannedWorkoutStatus.Planned })
        {
            return FromPlan(plannedToday, riskCategory == RiskCategory.Medium);
        }

        // 4. Ha az utolsó intenzív nap legalább két napja volt és alacsony a kockázat, minőségi edzés.
        var lastIntenseDay = recentWorkouts
            .Where(w => w.Rpe >= RiskCalculator.IntenseRpe)
            .Select(w => (DateOnly?)w.Date)
            .Max();
        var riskIsLow = riskCategory is null or RiskCategory.Low;
        if (riskIsLow && (lastIntenseDay is null || lastIntenseDay <= today.AddDays(-2)))
        {
            return QualityWorkout(goalType, goalDistanceKm);
        }

        // 5. Ha a héten (hétfőtől) még nem volt hosszú futás.
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        if (!recentWorkouts.Any(w => w.Type == WorkoutType.Long && w.Date >= weekStart && w.Date <= today))
        {
            return new(WorkoutType.Long, 4, 5, 90, null,
                "A héten még nem volt hosszú futás: nyugodt tempójú hosszú futás javasolt.");
        }

        // 6. Egyéb esetben könnyű futás.
        return new(WorkoutType.Easy, 3, 4, 45, null, "Könnyű, alapozó futás javasolt.");
    }

    /// <summary>
    /// A tervezett edzésből készült javaslat; közepes kockázatnál eggyel alacsonyabb RPE és 20%-kal rövidebb táv vagy idő.
    /// </summary>
    private static Recommendation FromPlan(PlannedWorkoutRecord planned, bool mediumRisk)
    {
        var factor = mediumRisk ? MediumRiskShortening : 1m;
        var rpe = mediumRisk ? Math.Max(1, planned.TargetRpe - 1) : planned.TargetRpe;

        // Ha csak táv van tervezve, az időtartamot 6 perc/km-es tempóval becsüljük.
        var plannedMinutes = planned.PlannedDurationSeconds is { } seconds
            ? seconds / 60m
            : (planned.PlannedDistanceKm ?? 0) * 6;
        var distance = planned.PlannedDistanceKm is { } km ? decimal.Round(km * factor, 1) : (decimal?)null;

        var reason = $"A mai napra tervezett edzés: {WorkoutTypeNames.Hungarian(planned.Type)}.";
        if (mediumRisk)
        {
            reason += " Közepes kockázat miatt eggyel alacsonyabb RPE-vel és 20%-kal rövidebben.";
        }

        return new(planned.Type, rpe, rpe, (int)Math.Round(plannedMinutes * factor), distance, reason);
    }

    /// <summary>
    /// Minőségi edzés RPE 7–8 között: 5 és 10 km-es célnál intervall, félmaratoni és maratoni célnál tempó.
    /// Egyéni célnál a táv dönt; cél nélkül tempó.
    /// </summary>
    private static Recommendation QualityWorkout(GoalType? goalType, decimal? goalDistanceKm)
    {
        var interval = goalType switch
        {
            GoalType.FiveK or GoalType.TenK => true,
            GoalType.HalfMarathon or GoalType.Marathon => false,
            GoalType.Custom => goalDistanceKm < TempoGoalMinDistanceKm,
            _ => false,
        };

        return interval
            ? new(WorkoutType.Interval, 7, 8, 50, null,
                "Az utolsó intenzív nap óta eltelt legalább két nap és alacsony a kockázat: intervall edzés javasolt a rövid távú célhoz.")
            : new(WorkoutType.Tempo, 7, 8, 45, null,
                "Az utolsó intenzív nap óta eltelt legalább két nap és alacsony a kockázat: tempófutás javasolt.");
    }
}
