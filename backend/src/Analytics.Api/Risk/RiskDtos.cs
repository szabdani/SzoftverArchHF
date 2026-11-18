namespace Analytics.Api.Risk;

/// <summary>Egy napi kockázati pontszám a kliens felé.</summary>
public record RiskScoreResponse(
    DateOnly Date,
    int Score,
    RiskCategory Category,
    List<string> Reasons,
    double? Acwr,
    double AcuteLoad,
    double ChronicLoad,
    decimal WeeklyKm,
    decimal PreviousWeeklyKm,
    DateTimeOffset CalculatedAt)
{
    /// <summary>
    /// Válasz-DTO az entitásból.
    /// </summary>
    public static RiskScoreResponse From(RiskScoreSnapshot s) => new(
        s.Date, s.Score, s.Category, s.Reasons, s.Acwr, s.AcuteLoad, s.ChronicLoad, s.WeeklyKm, s.PreviousWeeklyKm, s.CalculatedAt);
}

/// <summary>
/// A kockázati nézet (UC15): az aktuális pontszám és az alakulása dátum szerint növekvően.
/// Az aktuális null, ha a futóról még nincs számítás.
/// </summary>
public record RiskResponse(RiskScoreResponse? Current, List<RiskScoreResponse> History);
