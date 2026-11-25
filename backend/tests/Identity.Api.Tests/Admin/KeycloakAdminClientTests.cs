using System.Net;
using System.Text;
using Identity.Api.Admin;
using Microsoft.Extensions.Options;

namespace Identity.Api.Tests.Admin;

/// <summary>
/// A Keycloak Admin API kliens és a felhasználókezelés tesztjei egy hamis HTTP-kezelővel (UC16).
/// </summary>
public class KeycloakAdminClientTests
{
    private const string AdminBase = "http://kc/auth/admin/realms/futoedzes/";
    private static readonly string AnnaId = "11111111-1111-1111-1111-111111111111";
    private static readonly string BelaId = "22222222-2222-2222-2222-222222222222";

    /// <summary>
    /// Keresés: client credentials token a service account adataival, majd a felhasználók lekérése Bearer tokennel.
    /// </summary>
    [Fact]
    public async Task SearchUsers_UsesServiceAccountToken()
    {
        var handler = new FakeHandler();
        handler.On(HttpMethod.Get, "users?briefRepresentation=true&max=50&search=anna", $$"""[{"id":"{{AnnaId}}","username":"anna@x.hu","email":"anna@x.hu","firstName":"Anna","lastName":"Kiss","enabled":true}]""");

        var users = await Client(handler).SearchUsersAsync(" anna ", 50, CancellationToken.None);

        var token = handler.Requests[0];
        Assert.Equal("http://kc/auth/realms/futoedzes/protocol/openid-connect/token", token.Uri);
        Assert.Equal("grant_type=client_credentials&client_id=futoedzes-identity-admin&client_secret=titok", token.Body);
        Assert.Equal("Bearer test-token", handler.Requests[1].Authorization);
        Assert.Equal(new KeycloakUser(AnnaId, "anna@x.hu", "anna@x.hu", "Anna", "Kiss", true), Assert.Single(users));
    }

    /// <summary>
    /// A felhasználólistából kimarad a service account, és mindenki a legerősebb alkalmazás-szerepkörét kapja.
    /// A tokent egyszer kéri le, utána újrahasználja.
    /// </summary>
    [Fact]
    public async Task AdminSearch_FiltersServiceAccountAndPicksStrongestRole()
    {
        var handler = new FakeHandler();
        handler.On(HttpMethod.Get, "users?briefRepresentation=true&max=50", $$"""
            [{"id":"{{AnnaId}}","username":"anna","email":"anna@x.hu","firstName":"Anna","lastName":"Kiss","enabled":true},
             {"id":"{{BelaId}}","username":"bela","email":"bela@x.hu","firstName":"Béla","lastName":null,"enabled":false},
             {"id":"33333333-3333-3333-3333-333333333333","username":"service-account-futoedzes-identity-admin","enabled":true}]
            """);
        handler.On(HttpMethod.Get, $"users/{AnnaId}/role-mappings/realm/composite", """[{"id":"r","name":"runner"},{"id":"c","name":"coach"},{"id":"o","name":"offline_access"}]""");
        handler.On(HttpMethod.Get, $"users/{BelaId}/role-mappings/realm/composite", """[{"id":"o","name":"offline_access"}]""");

        var users = await new AdminUserService(Client(handler)).SearchAsync(null, CancellationToken.None);

        Assert.Equal(
            [new AdminUserResponse(Guid.Parse(AnnaId), "anna@x.hu", "Anna Kiss", "coach", true), new AdminUserResponse(Guid.Parse(BelaId), "bela@x.hu", "Béla", null, false)],
            users);
        Assert.Single(handler.Requests, r => r.Uri.EndsWith("/token"));
    }

    /// <summary>
    /// Szerepkörcsere: az alkalmazás-szerepkörök és az alapértelmezett csomag lekerül (más nem), az új közvetlenül rákerül.
    /// </summary>
    [Fact]
    public async Task SetAppRole_ReplacesAppRolesAndDefaultComposite()
    {
        var handler = new FakeHandler();
        handler.On(HttpMethod.Get, $"users/{AnnaId}/role-mappings/realm", """[{"id":"d","name":"default-roles-futoedzes"},{"id":"r","name":"runner"},{"id":"x","name":"egyeb"}]""");
        handler.On(HttpMethod.Delete, $"users/{AnnaId}/role-mappings/realm", "", HttpStatusCode.NoContent);
        handler.On(HttpMethod.Get, "roles/coach", """{"id":"c","name":"coach"}""");
        handler.On(HttpMethod.Post, $"users/{AnnaId}/role-mappings/realm", "", HttpStatusCode.NoContent);

        var ok = await Client(handler).SetAppRoleAsync(AnnaId, "coach", AdminUserService.AppRoles, CancellationToken.None);

        Assert.True(ok);
        Assert.Equal("""[{"id":"d","name":"default-roles-futoedzes"},{"id":"r","name":"runner"}]""", handler.Single(HttpMethod.Delete).Body);
        Assert.Equal("""[{"id":"c","name":"coach"}]""", handler.Single(HttpMethod.Post, "role-mappings").Body);
    }

    /// <summary>
    /// Nem létező felhasználó: a szerepkörcsere és a letiltás is false-t ad.
    /// </summary>
    [Fact]
    public async Task UnknownUser_ReturnsFalse()
    {
        var handler = new FakeHandler();
        handler.On(HttpMethod.Get, $"users/{AnnaId}/role-mappings/realm", "", HttpStatusCode.NotFound);
        handler.On(HttpMethod.Put, $"users/{AnnaId}", "", HttpStatusCode.NotFound);

        Assert.False(await Client(handler).SetAppRoleAsync(AnnaId, "coach", AdminUserService.AppRoles, CancellationToken.None));
        Assert.False(await Client(handler).SetEnabledAsync(AnnaId, false, CancellationToken.None));
    }

    /// <summary>
    /// Letiltás: PUT a felhasználóra csak az enabled mezővel.
    /// </summary>
    [Fact]
    public async Task SetEnabled_SendsOnlyEnabledFlag()
    {
        var handler = new FakeHandler();
        handler.On(HttpMethod.Put, $"users/{AnnaId}", "", HttpStatusCode.NoContent);

        Assert.True(await Client(handler).SetEnabledAsync(AnnaId, false, CancellationToken.None));
        Assert.Equal("""{"enabled":false}""", handler.Single(HttpMethod.Put).Body);
    }

    /// <summary>
    /// Ha a megjegyzett (lejárt) token 401-et ad, a kliens friss tokent kér és egyszer újrapróbálja a
    /// kérést; a második próbálkozás sikeres. Így az admin felhasználólista nem esik szét a rövid életű
    /// service account token lejárta miatt.
    /// </summary>
    [Fact]
    public async Task ExpiredToken_RefreshesAndRetriesOnce()
    {
        var handler = new FakeHandler();
        handler.OnFirstCallUnauthorized(HttpMethod.Get, "users?briefRepresentation=true&max=50");
        handler.On(HttpMethod.Get, "users?briefRepresentation=true&max=50", $$"""[{"id":"{{AnnaId}}","username":"anna@x.hu","email":"anna@x.hu","firstName":"Anna","lastName":"Kiss","enabled":true}]""");

        var users = await Client(handler).SearchUsersAsync(null, 50, CancellationToken.None);

        Assert.Single(users);
        // Két token-kérés (az eredeti és a 401 utáni frissítés) és két users-hívás (401, majd siker).
        Assert.Equal(2, handler.Requests.Count(r => r.Uri.EndsWith("/token")));
        Assert.Equal(2, handler.Requests.Count(r => r.Uri.Contains("users?briefRepresentation")));
    }

    /// <summary>
    /// A tesztelt kliens a hamis kezelővel és rögzített beállításokkal.
    /// </summary>
    private static KeycloakAdminClient Client(FakeHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new KeycloakAdminOptions { BaseUrl = "http://kc/auth", ClientSecret = "titok" }));

    /// <summary>
    /// Hamis HTTP-kezelő: a token végpontra mindig tokent ad, a többi kérésre a beállított választ;
    /// minden kérést megjegyez.
    /// </summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Dictionary<(HttpMethod, string), (string Body, HttpStatusCode Status)> responses = [];
        private readonly HashSet<(HttpMethod, string)> unauthorizeFirstCall = [];

        public List<(HttpMethod Method, string Uri, string Body, string? Authorization)> Requests { get; } = [];

        /// <summary>
        /// Válasz beállítása egy Admin API útvonalra (a realm Admin API alapcíméhez képest).
        /// </summary>
        public void On(HttpMethod method, string path, string body, HttpStatusCode status = HttpStatusCode.OK) =>
            responses[(method, AdminBase + path)] = (body, status);

        /// <summary>
        /// Egy útvonal első hívása 401-et ad (mintha a megjegyzett token lejárt volna); a következő hívásra
        /// a beállított válasz jön. A token lejárásának szimulálásához.
        /// </summary>
        public void OnFirstCallUnauthorized(HttpMethod method, string path) =>
            unauthorizeFirstCall.Add((method, AdminBase + path));

        /// <summary>
        /// Az egyetlen, adott metódusú (és opcionálisan útvonalrészletet tartalmazó) kérés.
        /// </summary>
        public (HttpMethod Method, string Uri, string Body, string? Authorization) Single(HttpMethod method, string? contains = null) =>
            Requests.Single(r => r.Method == method && (contains is null || r.Uri.Contains(contains)) && !r.Uri.EndsWith("/token"));

        /// <summary>
        /// A kérés megjegyzése és a beállított válasz visszaadása.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!.ToString();
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, uri, body, request.Headers.Authorization?.ToString()));

            if (uri.EndsWith("/protocol/openid-connect/token"))
            {
                return Json("""{"access_token":"test-token","expires_in":300}""", HttpStatusCode.OK);
            }

            // A lejárt token szimulálása: az első hívás 401, utána a beállított válasz jön.
            if (unauthorizeFirstCall.Remove((request.Method, uri)))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return responses.TryGetValue((request.Method, uri), out var response)
                ? Json(response.Body, response.Status)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent($"Nincs beállítva: {request.Method} {uri}") };
        }

        /// <summary>
        /// JSON válasz a megadott státusszal.
        /// </summary>
        private static HttpResponseMessage Json(string body, HttpStatusCode status) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
