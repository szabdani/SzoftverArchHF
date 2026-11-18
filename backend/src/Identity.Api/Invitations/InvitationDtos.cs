namespace Identity.Api.Invitations;

/// <summary>
/// Meghívás kérése: a meghívandó futó e-mail címe.
/// </summary>
public record InvitationRequest(string? Email);

/// <summary>
/// Egy meghívás vagy élő kapcsolat a kliens felé.
/// </summary>
public record InvitationResponse(
    Guid Id,
    Guid CoachId,
    string CoachEmail,
    string RunnerEmail,
    Guid? RunnerId,
    InvitationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>
    /// Válasz-DTO az entitásból.
    /// </summary>
    public static InvitationResponse From(Invitation i) =>
        new(i.Id, i.CoachId, i.CoachEmail, i.RunnerEmail, i.RunnerId, i.Status, i.CreatedAt, i.UpdatedAt);
}
