namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások a kliens felé és vissza (lekérdezés és teljes felülírás).
/// </summary>
public record RuleSettingsDto(
    double AcwrCritical,
    double AcwrElevated,
    int WeeklyKmIncreasePercent,
    int IntenseRpe,
    int IntenseDaysPerWeek,
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
