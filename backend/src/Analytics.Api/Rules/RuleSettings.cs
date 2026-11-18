using Analytics.Api.Persistence;

namespace Analytics.Api.Rules;

/// <summary>
/// A kockázati és figyelmeztetési szabályok küszöbei és aktív állapota (UC17). Egyetlen sor; ha még
/// nincs elmentve, a spec alapértelmezései érvényesek. Ugyanezek vezérlik a kockázati pontszámot
/// és a figyelmeztetéseket is.
/// </summary>
public class RuleSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>E fölött az ACWR kritikus (spec: 1,5).</summary>
    public double AcwrCritical { get; set; } = 1.5;

    /// <summary>Ettől az ACWR-től emelkedő a terhelés (spec: 1,3).</summary>
    public double AcwrElevated { get; set; } = 1.3;

    /// <summary>Ennyi százaléknál nagyobb heti km-növekedés számít (spec: 10%).</summary>
    public int WeeklyKmIncreasePercent { get; set; } = 10;

    /// <summary>Ettől az RPE-től intenzív egy nap (spec: 7).</summary>
    public int IntenseRpe { get; set; } = 7;

    /// <summary>Ennyi intenzív nap 7 napon belül már túl sűrű (spec: 3; csak figyelmeztetés).</summary>
    public int IntenseDaysPerWeek { get; set; } = 3;

    /// <summary>Ennyi kihagyott tervezett edzés 7 napon belül számít (spec: 2).</summary>
    public int MissedWorkoutsPerWeek { get; set; } = 2;

    public bool AcwrEnabled { get; set; } = true;
    public bool WeeklyKmIncreaseEnabled { get; set; } = true;
    public bool IntenseDaysEnabled { get; set; } = true;
    public bool NoRestDayEnabled { get; set; } = true;
    public bool MissedWorkoutsEnabled { get; set; } = true;

    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// A beállítások betöltése az adatbázisból.
/// </summary>
public static class RuleSettingsExtensions
{
    /// <summary>
    /// Az elmentett beállítások, vagy ha még nincsenek, a spec alapértelmezései.
    /// </summary>
    public static async Task<RuleSettings> GetRuleSettingsAsync(this AnalyticsDbContext db, CancellationToken ct) =>
        await db.RuleSettings.FindAsync([RuleSettings.SingletonId], ct) ?? new RuleSettings();
}
