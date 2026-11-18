using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Invitations;

/// <summary>
/// Meghívás kérése: a meghívandó futó e-mail címe. A mezőszabályok az attribútumokban vannak (közös
/// séma a frontenddel).
/// </summary>
public record InvitationRequest(
    [property: Required(ErrorMessage = "Az e-mail cím megadása kötelező.")]
    [property: EmailAddress(ErrorMessage = "Érvénytelen e-mail cím.")]
    [property: MaxLength(Invitation.EmailMaxLength, ErrorMessage = "Az e-mail cím túl hosszú.")]
    string? Email);

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
