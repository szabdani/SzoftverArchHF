using System.Security.Claims;

namespace Identity.Api.Invitations;

/// <summary>
/// Edzői meghívások és edző–futó kapcsolatok kezelése (UC12, UC13).
/// </summary>
public interface IInvitationService
{
    /// <summary>
    /// Új meghívás a bejelentkezett edzőtől a megadott, már ellenőrzött e-mail címre.
    /// </summary>
    /// <returns>A meghívás, vagy null, ha erre a címre már van függő meghívás vagy élő kapcsolat.</returns>
    Task<InvitationResponse?> CreateAsync(ClaimsPrincipal coach, string runnerEmail, CancellationToken ct);

    /// <summary>
    /// Az edző által küldött összes meghívás, a legfrissebb elöl.
    /// </summary>
    Task<List<InvitationResponse>> ListSentAsync(ClaimsPrincipal coach, CancellationToken ct);

    /// <summary>
    /// A futó e-mail címére érkezett, még megválaszolatlan meghívások.
    /// </summary>
    Task<List<InvitationResponse>> ListReceivedAsync(ClaimsPrincipal runner, CancellationToken ct);

    /// <summary>
    /// A futó elfogadja a meghívást, létrejön a kapcsolat.
    /// </summary>
    /// <returns>Az elfogadott meghívás, vagy null, ha nincs ilyen függő meghívás a futó címére.</returns>
    Task<InvitationResponse?> AcceptAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct);

    /// <summary>
    /// A futó elutasítja a meghívást.
    /// </summary>
    /// <returns>true, ha elutasította; false, ha nincs ilyen függő meghívás a futó címére.</returns>
    Task<bool> DeclineAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct);

    /// <summary>
    /// A felhasználó élő kapcsolatai: futónál az edzői, edzőnél a futói.
    /// </summary>
    Task<List<InvitationResponse>> ListLinksAsync(ClaimsPrincipal user, CancellationToken ct);

    /// <summary>
    /// A futó megszünteti egy élő kapcsolatát.
    /// </summary>
    /// <returns>true, ha megszüntette; false, ha nincs ilyen élő kapcsolata.</returns>
    Task<bool> EndLinkAsync(ClaimsPrincipal runner, Guid id, CancellationToken ct);
}
