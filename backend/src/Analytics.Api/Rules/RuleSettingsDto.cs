using System.ComponentModel.DataAnnotations;

namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások a kliens felé és vissza (lekérdezés és teljes felülírás). A tartományok az
/// attribútumokban vannak (közös séma a frontenddel).
/// </summary>
public record RuleSettingsDto(
    [property: Range(0.5, 5, ErrorMessage = "A kritikus ACWR 0,5 és 5 között lehet.")]
    double AcwrCritical,
    [property: Range(0.5, 5, ErrorMessage = "Az emelkedő ACWR 0,5 és 5 között lehet.")]
    double AcwrElevated,
    [property: Range(1, 100, ErrorMessage = "A heti km-növekedés küszöbe 1 és 100% között lehet.")]
    int WeeklyKmIncreasePercent,
    [property: Range(1, 10, ErrorMessage = "Az intenzív RPE 1 és 10 között lehet.")]
    int IntenseRpe,
    [property: Range(2, 7, ErrorMessage = "A heti intenzív napok küszöbe 2 és 7 között lehet.")]
    int IntenseDaysPerWeek,
    [property: Range(1, 7, ErrorMessage = "A heti kihagyott edzések küszöbe 1 és 7 között lehet.")]
    int MissedWorkoutsPerWeek,
    bool AcwrEnabled,
    bool WeeklyKmIncreaseEnabled,
    bool IntenseDaysEnabled,
    bool NoRestDayEnabled,
    bool MissedWorkoutsEnabled,
    DateTimeOffset? UpdatedAt = null)
{
    /// <summary>
    /// DTO az entitásból.
    /// </summary>
    public static RuleSettingsDto From(RuleSettings s) => new(
        s.AcwrCritical, s.AcwrElevated, s.WeeklyKmIncreasePercent, s.IntenseRpe, s.IntenseDaysPerWeek, s.MissedWorkoutsPerWeek,
        s.AcwrEnabled, s.WeeklyKmIncreaseEnabled, s.IntenseDaysEnabled, s.NoRestDayEnabled, s.MissedWorkoutsEnabled, s.UpdatedAt);

    /// <summary>
    /// Az értékek beírása az entitásba (az időbélyeg kivételével).
    /// </summary>
    public void ApplyTo(RuleSettings s)
    {
        s.AcwrCritical = AcwrCritical;
        s.AcwrElevated = AcwrElevated;
        s.WeeklyKmIncreasePercent = WeeklyKmIncreasePercent;
        s.IntenseRpe = IntenseRpe;
        s.IntenseDaysPerWeek = IntenseDaysPerWeek;
        s.MissedWorkoutsPerWeek = MissedWorkoutsPerWeek;
        s.AcwrEnabled = AcwrEnabled;
        s.WeeklyKmIncreaseEnabled = WeeklyKmIncreaseEnabled;
        s.IntenseDaysEnabled = IntenseDaysEnabled;
        s.NoRestDayEnabled = NoRestDayEnabled;
        s.MissedWorkoutsEnabled = MissedWorkoutsEnabled;
    }
}
