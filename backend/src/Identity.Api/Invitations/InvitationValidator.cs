using System.Net.Mail;
using BuildingBlocks.Validation;

namespace Identity.Api.Invitations;

/// <summary>
/// A meghívás kérésének ellenőrzése. A mezőszabályok az <see cref="InvitationRequest"/> attribútumaiban
/// vannak (közös séma a frontenddel); itt csak a szerveroldali szabályok vannak.
/// </summary>
public static class InvitationValidator
{
    /// <summary>
    /// Ellenőrzi a futó e-mail címét. Siker esetén a normalizált (kisbetűs) címet adja vissza,
    /// hiba esetén a magyar hibaüzenetet.
    /// </summary>
    /// <param name="coachEmail">A meghívó edző e-mail címe, hogy saját magát ne hívhassa meg.</param>
    public static bool TryValidate(
        InvitationRequest request,
        string? coachEmail,
        out string runnerEmail,
        out Dictionary<string, string[]> errors)
    {
        runnerEmail = NormalizeEmail(request.Email);
        errors = RequestValidation.ValidateAnnotations(request);

        // Szerveroldali szabályok: puszta cím kell (a "Név <cím>" alakot az attribútum még elfogadná),
        // és az edző saját magát nem hívhatja meg.
        if (!MailAddress.TryCreate(runnerEmail, out var address) || address.Address != runnerEmail)
        {
            errors.TryAdd("email", ["Érvénytelen e-mail cím."]);
        }
        else if (runnerEmail == NormalizeEmail(coachEmail))
        {
            errors.TryAdd("email", ["Saját magadat nem hívhatod meg."]);
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// E-mail cím összehasonlítható alakja: a szélső szóközök nélkül, kisbetűsen.
    /// </summary>
    public static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
