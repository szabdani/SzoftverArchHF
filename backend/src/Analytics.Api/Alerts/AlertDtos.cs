namespace Analytics.Api.Alerts;

/// <summary>Egy figyelmeztetés a kliens felé.</summary>
public record AlertResponse(
    Guid Id,
    Guid RunnerId,
    AlertRule Rule,
    AlertSeverity Severity,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcknowledgedAt)
{
    /// <summary>
    /// Válasz-DTO az entitásból.
    /// </summary>
    public static AlertResponse From(Alert a) => new(a.Id, a.RunnerId, a.Rule, a.Severity, a.Message, a.CreatedAt, a.AcknowledgedAt);
}
