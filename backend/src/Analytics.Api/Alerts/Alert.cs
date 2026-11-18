namespace Analytics.Api.Alerts;

/// <summary>Figyelmeztetési szabályok (spec 3.6).</summary>
public enum AlertRule
{
    SuddenLoadIncrease,
    RisingLoad,
    DenseIntenseDays,
    MissedWorkout,
    FrequentMissedWorkouts,
    NoRestDay,
}

/// <summary>A figyelmeztetés fokozata: információ, figyelmeztetés vagy kritikus.</summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Critical,
}

/// <summary>
/// Egy szabály teljesülésekor keletkező figyelmeztetés (UC09). Ugyanaz a szabály ugyanarra az
/// időszakra (<see cref="PeriodKey"/>) csak egyszer keletkezik. A futó és a hozzárendelt edző is látja.
/// </summary>
public class Alert
{
    public Guid Id { get; set; }
    public Guid RunnerId { get; set; }
    public AlertRule Rule { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = "";

    /// <summary>Az időszak azonosítója az ismétlődés kiszűréséhez: ISO hét (pl. 2026-W47) vagy egy tervezett edzés.</summary>
    public string PeriodKey { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
