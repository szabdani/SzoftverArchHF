using System.Globalization;
using Analytics.Api.ReadModels;

namespace Analytics.Api.Risk;

/// <summary>Kockázati kategória (spec 3.8): 0–29 alacsony, 30–59 közepes, 60–100 magas.</summary>
public enum RiskCategory
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Egy számítás eredménye: a terhelési mutatók, a pontszám, a kategória és a teljesült szabályok
/// szöveges indoklása.
/// </summary>
public record RiskResult(
    double AcuteLoad,
    double ChronicLoad,
    double? Acwr,
    decimal WeeklyKm,
    decimal PreviousWeeklyKm,
    int Score,
    RiskCategory Category,
    List<string> Reasons);

/// <summary>
/// Szabályalapú túlterhelési kockázati pontszám (spec 3.8). Determinisztikus, I/O nélküli számítás,
/// ezért egységtesztekkel ellenőrizhető. A küszöbök a spec alapértelmezései.
/// </summary>
public static class RiskCalculator
{
    public const double AcwrCritical = 1.5;
    public const double AcwrElevated = 1.3;
    public const decimal WeeklyKmIncreaseLimit = 0.10m;
    public const int IntenseRpe = 7;
    public const int MissedWorkoutLimit = 2;

    public const int AcwrCriticalPoints = 40;
    public const int AcwrElevatedPoints = 20;
    public const int WeeklyKmIncreasePoints = 20;
    public const int ConsecutiveIntenseDaysPoints = 20;
    public const int NoRestDayPoints = 10;
    public const int MissedWorkoutsPoints = 10;
    public const int MaxScore = 100;

    private static readonly CultureInfo Hungarian = CultureInfo.GetCultureInfo("hu-HU");

    /// <summary>
    /// Kiszámolja a terhelési mutatókat és a kockázati pontszámot a megadott napra.
    /// </summary>
    /// <param name="workouts">A futó edzései; a 28 napos ablakon kívülieket figyelmen kívül hagyja.</param>
    /// <param name="missedLast7Days">A kihagyott tervezett edzések száma az utolsó 7 napban.</param>
    /// <param name="today">A nap, amire számolunk (a felhasználó időzónájában).</param>
    /// <param name="firstWorkoutDate">A futó legelső edzésének napja; ebből tudjuk, van-e már 28 napnyi adat.</param>
    public static RiskResult Calculate(
        IReadOnlyCollection<WorkoutRecord> workouts, int missedLast7Days, DateOnly today, DateOnly? firstWorkoutDate)
    {
        // Ablakok, mindkét végükön inkluzívak: utolsó 7 nap [ma−6, ma], előző 7 nap [ma−13, ma−7],
        // utolsó 28 nap [ma−27, ma].
        var last7Start = today.AddDays(-6);
        var previous7Start = today.AddDays(-13);
        var last28Start = today.AddDays(-27);

        var last7 = workouts.Where(w => w.Date >= last7Start && w.Date <= today).ToList();
        var previous7 = workouts.Where(w => w.Date >= previous7Start && w.Date < last7Start).ToList();
        var last28 = workouts.Where(w => w.Date >= last28Start && w.Date <= today).ToList();

        // Akut terhelés: az utolsó 7 nap összege; krónikus: az utolsó 28 nap heti átlaga.
        var acuteLoad = last7.Sum(w => w.Load);
        var chronicLoad = last28.Sum(w => w.Load) / 4;

        // Az ACWR-t csak 28 napnyi adat után számoljuk, különben az első hetek torzan magas értéket adnának.
        // Két tizedesre kerekítjük, hogy a kiírt és a szabályban használt érték ugyanaz legyen.
        double? acwr = firstWorkoutDate <= last28Start && chronicLoad > 0
            ? Math.Round(acuteLoad / chronicLoad, 2, MidpointRounding.AwayFromZero)
            : null;

        var weeklyKm = last7.Sum(w => w.DistanceKm);
        var previousWeeklyKm = previous7.Sum(w => w.DistanceKm);

        var score = 0;
        var reasons = new List<string>();

        // Az ACWR két szabálya kizárja egymást: a magasabb sáv ér többet.
        if (acwr > AcwrCritical)
        {
            score += AcwrCriticalPoints;
            reasons.Add($"ACWR {Format(acwr.Value)} > 1,5");
        }
        else if (acwr >= AcwrElevated)
        {
            score += AcwrElevatedPoints;
            reasons.Add($"ACWR {Format(acwr.Value)} az 1,3–1,5 sávban");
        }

        // Az előző hét nulla távjához képest nincs értelmezhető százalékos növekedés.
        if (previousWeeklyKm > 0 && weeklyKm > previousWeeklyKm * (1 + WeeklyKmIncreaseLimit))
        {
            score += WeeklyKmIncreasePoints;
            var increasePercent = (weeklyKm / previousWeeklyKm - 1) * 100;
            reasons.Add($"Heti táv {Format(weeklyKm)} km, {increasePercent.ToString("0", Hungarian)}%-kal több az előző hétnél ({Format(previousWeeklyKm)} km)");
        }

        // Intenzív nap: legalább egy RPE ≥ 7 edzés. Keressük az első olyan napot, amit egy intenzív nap követ.
        var intenseDays = last7.Where(w => w.Rpe >= IntenseRpe).Select(w => w.Date).ToHashSet();
        var firstOfPair = intenseDays.Order().Where(day => intenseDays.Contains(day.AddDays(1))).Select(day => (DateOnly?)day).FirstOrDefault();
        if (firstOfPair is { } day)
        {
            score += ConsecutiveIntenseDaysPoints;
            reasons.Add($"Két egymást követő intenzív nap (RPE ≥ 7): {day.ToString("MM.dd.", Hungarian)} és {day.AddDays(1).ToString("MM.dd.", Hungarian)}");
        }

        // Pihenőnap hiánya: az utolsó 7 nap mindegyikén volt edzés.
        if (last7.Select(w => w.Date).Distinct().Count() == 7)
        {
            score += NoRestDayPoints;
            reasons.Add("7 egymást követő edzésnap, nincs pihenőnap");
        }

        if (missedLast7Days >= MissedWorkoutLimit)
        {
            score += MissedWorkoutsPoints;
            reasons.Add($"{missedLast7Days} kihagyott tervezett edzés az utolsó 7 napban");
        }

        score = Math.Min(score, MaxScore);
        return new RiskResult(acuteLoad, chronicLoad, acwr, weeklyKm, previousWeeklyKm, score, CategoryFor(score), reasons);
    }

    /// <summary>
    /// A pontszám kategóriája: 0–29 alacsony, 30–59 közepes, 60–100 magas.
    /// </summary>
    public static RiskCategory CategoryFor(int score) => score switch
    {
        < 30 => RiskCategory.Low,
        < 60 => RiskCategory.Medium,
        _ => RiskCategory.High,
    };

    /// <summary>
    /// Szám magyar formában, két tizedessel (pl. 1,62).
    /// </summary>
    private static string Format(double value) => value.ToString("0.00", Hungarian);

    /// <summary>
    /// Táv magyar formában, egy tizedessel (pl. 32,5).
    /// </summary>
    private static string Format(decimal value) => value.ToString("0.0", Hungarian);
}
