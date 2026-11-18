namespace Analytics.Api.Risk;

/// <summary>
/// Egy futó kockázati pontszáma egy napra, a mutatókkal és az indoklásokkal együtt. Futónként és
/// naponként egy van; a nap folyamán minden újraszámolás felülírja, így az alakulása napról napra
/// visszakereshető (spec 3.8).
/// </summary>
public class RiskScoreSnapshot
{
    public Guid Id { get; set; }
    public Guid RunnerId { get; set; }
    public DateOnly Date { get; set; }
    public int Score { get; set; }
    public RiskCategory Category { get; set; }
    public List<string> Reasons { get; set; } = [];
    public double? Acwr { get; set; }
    public double AcuteLoad { get; set; }
    public double ChronicLoad { get; set; }
    public decimal WeeklyKm { get; set; }
    public decimal PreviousWeeklyKm { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }

    /// <summary>
    /// Egy számítás eredményének beírása.
    /// </summary>
    public void Apply(RiskResult result, DateTimeOffset now)
    {
        Score = result.Score;
        Category = result.Category;
        Reasons = result.Reasons;
        Acwr = result.Acwr;
        AcuteLoad = result.AcuteLoad;
        ChronicLoad = result.ChronicLoad;
        WeeklyKm = result.WeeklyKm;
        PreviousWeeklyKm = result.PreviousWeeklyKm;
        CalculatedAt = now;
    }
}
