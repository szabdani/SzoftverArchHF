using System.Security.Claims;
using BuildingBlocks.Auth;

namespace Identity.Api.Invitations;

/// <summary>
/// A meghívások és kapcsolatok HTTP végpontjai. Csak a kérés ellenőrzését és a HTTP választ
/// intézik, a logika a service-ben van.
/// </summary>
public static class InvitationEndpoints
{
    /// <summary>
    /// Regisztrálja a végpontokat a /api/identity útvonalon.
    /// </summary>
    public static void MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity");

        // UC12: az edző hív meg futót.
        group.MapPost("/invitations", CreateAsync).RequireAuthorization(Policies.Coach).Produces<InvitationResponse>().ProducesValidationProblem();
        group.MapGet("/invitations/sent", ListSentAsync).RequireAuthorization(Policies.Coach).Produces<List<InvitationResponse>>();

        // UC13: a futó fogadja el vagy utasítja el a meghívást, és ő szüntetheti meg a kapcsolatot.
        group.MapGet("/invitations/received", ListReceivedAsync).RequireAuthorization(Policies.Runner).Produces<List<InvitationResponse>>();
        group.MapPost("/invitations/{id:guid}/accept", AcceptAsync).RequireAuthorization(Policies.Runner).Produces<InvitationResponse>();
        group.MapPost("/invitations/{id:guid}/decline", DeclineAsync).RequireAuthorization(Policies.Runner).Produces(StatusCodes.Status204NoContent);
        group.MapGet("/links", ListLinksAsync).RequireAuthorization(Policies.RunnerOrCoach).Produces<List<InvitationResponse>>();
        group.MapDelete("/links/{id:guid}", EndLinkAsync).RequireAuthorization(Policies.Runner).Produces(StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// POST /api/identity/invitations – futó meghívása e-mail cím alapján.
    /// </summary>
    private static async Task<IResult> CreateAsync(
        InvitationRequest request, ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        if (!InvitationValidator.TryValidate(request, user.GetEmail(), out var runnerEmail, out var errors))
        {
            return Results.ValidationProblem(errors);
        }

        var invitation = await service.CreateAsync(user, runnerEmail, ct);
        return invitation is null
            ? Results.Problem(
                title: "Erre a címre már van függő meghívásod vagy élő kapcsolatod.",
                statusCode: StatusCodes.Status409Conflict)
            : Results.Ok(invitation);
    }

    /// <summary>
    /// GET /api/identity/invitations/sent – az edző által küldött meghívások.
    /// </summary>
    private static async Task<IResult> ListSentAsync(ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        return Results.Ok(await service.ListSentAsync(user, ct));
    }

    /// <summary>
    /// GET /api/identity/invitations/received – a futónak szóló, függő meghívások.
    /// </summary>
    private static async Task<IResult> ListReceivedAsync(ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        return Results.Ok(await service.ListReceivedAsync(user, ct));
    }

    /// <summary>
    /// POST /api/identity/invitations/{id}/accept – meghívás elfogadása.
    /// </summary>
    private static async Task<IResult> AcceptAsync(Guid id, ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        var invitation = await service.AcceptAsync(user, id, ct);
        return invitation is null ? Results.NotFound() : Results.Ok(invitation);
    }

    /// <summary>
    /// POST /api/identity/invitations/{id}/decline – meghívás elutasítása.
    /// </summary>
    private static async Task<IResult> DeclineAsync(Guid id, ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        var declined = await service.DeclineAsync(user, id, ct);
        return declined ? Results.NoContent() : Results.NotFound();
    }

    /// <summary>
    /// GET /api/identity/links – élő kapcsolatok (futónál az edzők, edzőnél a futók).
    /// </summary>
    private static async Task<IResult> ListLinksAsync(ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        return Results.Ok(await service.ListLinksAsync(user, ct));
    }

    /// <summary>
    /// DELETE /api/identity/links/{id} – a futó megszünteti a kapcsolatot.
    /// </summary>
    private static async Task<IResult> EndLinkAsync(Guid id, ClaimsPrincipal user, IInvitationService service, CancellationToken ct)
    {
        var ended = await service.EndLinkAsync(user, id, ct);
        return ended ? Results.NoContent() : Results.NotFound();
    }
}
