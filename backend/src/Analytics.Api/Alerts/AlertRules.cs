using System.Globalization;
using Analytics.Api.ReadModels;
using Analytics.Api.Risk;

namespace Analytics.Api.Alerts;

/// <summary>Egy teljesült szabály, amiből figyelmeztetés lesz, ha az időszakra még nincs ilyen.</summary>
public record AlertCandidate(AlertRule Rule, AlertSeverity Severity, string Message, string PeriodKey);

/// <summary>
/// A figyelmeztetési szabályok (spec 3.6) kiértékelése a kockázati számítás mutatóiból.
/// Determinisztikus, I/O nélküli, ezért egységtesztekkel ellenőrizhető.
/// </summary>
public static class AlertRules
{
    public const int IntenseDaysPerWeekLimit = 3;

    private static readonly CultureInfo Hungarian = CultureInfo.GetCultureInfo("hu-HU");

    /// <summary>
    /// A mai napra teljesülő szabályok. A heti szabályok időszaka az ISO hét, a kihagyott edzésé maga
    /// a tervezett edzés, így ugyanarra nem keletkezik ismételt figyelmeztetés.
    /// </summary>
    /// <param name="result">A kockázati számítás eredménye a mai napra.</param>
    /// <param name="missedLast7Days">Az utolsó 7 nap kihagyott tervezett edzései.</param>
    /// <param name="today">A mai nap a felhasználó időzónájában.</param>
    public static List<AlertCandidate> Evaluate(RiskResult result, IReadOnlyCollection<PlannedWorkoutRecord> missedLast7Days, DateOnly today)
    {
        var week = IsoWeekKey(today);
        var candidates = new List<AlertCandidate>();

        // Terhelés: az ACWR két sávja kizárja egymást; a heti km-növekedés az ACWR-től függetlenül
        // is emelkedő terhelés.
        var risingReasons = new List<string>();
        if (result.Acwr > RiskCalculator.AcwrCritical)
        {
            candidates.Add(new(AlertRule.SuddenLoadIncrease, AlertSeverity.Critical,
                $"Hirtelen terhelésnövekedés: ACWR {result.Acwr.Value.ToString("0.00", Hungarian)} > 1,5", week));
        }
        else if (result.Acwr >= RiskCalculator.AcwrElevated)
        {
            risingReasons.Add($"ACWR {result.Acwr.Value.ToString("0.00", Hungarian)} az 1,3–1,5 sávban");
        }

        if (result.PreviousWeeklyKm > 0 && result.WeeklyKm > result.PreviousWeeklyKm * (1 + RiskCalculator.WeeklyKmIncreaseLimit))
        {
            var percent = (result.WeeklyKm / result.PreviousWeeklyKm - 1) * 100;
            risingReasons.Add($"a heti táv {percent.ToString("0", Hungarian)}%-kal nőtt");
        }

        if (risingReasons.Count > 0)
        {
            candidates.Add(new(AlertRule.RisingLoad, AlertSeverity.Warning,
                $"Emelkedő terhelés: {string.Join(", ", risingReasons)}", week));
        }

        // Túl sűrű intenzív napok: két egymást követő, vagy legalább 3 intenzív nap 7 napon belül.
        if (result.ConsecutiveIntenseStart is { } start)
        {
            candidates.Add(new(AlertRule.DenseIntenseDays, AlertSeverity.Warning,
                $"Túl sűrű intenzív napok: {start.ToString("MM.dd.", Hungarian)} és {start.AddDays(1).ToString("MM.dd.", Hungarian)} is RPE ≥ 7", week));
        }
        else if (result.IntenseDaysLast7 >= IntenseDaysPerWeekLimit)
        {
            candidates.Add(new(AlertRule.DenseIntenseDays, AlertSeverity.Warning,
                $"Túl sűrű intenzív napok: {result.IntenseDaysLast7} intenzív nap az utolsó 7 napban", week));
        }

        // Minden kihagyott edzésről egy információ, hetente legalább kettőnél figyelmeztetés is.
        foreach (var missed in missedLast7Days)
        {
            candidates.Add(new(AlertRule.MissedWorkout, AlertSeverity.Info,
                $"Kihagyott edzés: {missed.Date.ToString("MM.dd.", Hungarian)} ({WorkoutTypeNames.Hungarian(missed.Type)})", $"planned:{missed.Id}"));
        }

        if (missedLast7Days.Count >= RiskCalculator.MissedWorkoutLimit)
        {
            candidates.Add(new(AlertRule.FrequentMissedWorkouts, AlertSeverity.Warning,
                $"{missedLast7Days.Count} kihagyott tervezett edzés az utolsó 7 napban", week));
        }

        if (result.NoRestDay)
        {
            candidates.Add(new(AlertRule.NoRestDay, AlertSeverity.Info,
                "Pihenőnap hiánya: 7 egymást követő edzésnap", week));
        }

        return candidates;
    }

    /// <summary>
    /// Az ISO hét azonosítója, pl. 2026-W47.
    /// </summary>
    public static string IsoWeekKey(DateOnly day)
    {
        var date = day.ToDateTime(TimeOnly.MinValue);
        return $"{ISOWeek.GetYear(date)}-W{ISOWeek.GetWeekOfYear(date):00}";
    }
}
