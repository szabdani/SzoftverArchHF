using System.Net.Mail;

namespace Identity.Api.Invitations;

/// <summary>
/// A meghívás kérésének ellenőrzése. A hibakulcsok a JSON mezőnevek (camelCase).
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
        errors = new Dictionary<string, string[]>();

        if (runnerEmail.Length == 0)
        {
            errors["email"] = ["Az e-mail cím megadása kötelező."];
        }
        else if (runnerEmail.Length > Invitation.EmailMaxLength
            || !MailAddress.TryCreate(runnerEmail, out var address)
            || address.Address != runnerEmail)
        {
            // A MailAddress a "Név <cím>" alakot is elfogadná, ezért a puszta címet várjuk el.
            errors["email"] = ["Érvénytelen e-mail cím."];
        }
        else if (runnerEmail == NormalizeEmail(coachEmail))
        {
            errors["email"] = ["Saját magadat nem hívhatod meg."];
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// E-mail cím összehasonlítható alakja: a szélső szóközök nélkül, kisbetűsen.
    /// </summary>
    public static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
