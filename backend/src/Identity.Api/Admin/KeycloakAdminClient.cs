using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Identity.Api.Admin;

/// <summary>Egy Keycloak felhasználó (a felhasználókezeléshez szükséges mezők).</summary>
public record KeycloakUser(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled);

/// <summary>Egy Keycloak realm szerepkör.</summary>
public record KeycloakRole(string Id, string Name);

/// <summary>
/// Vékony kliens a Keycloak Admin REST API-hoz (UC16). A tokent client credentials folyamattal
/// kéri a service account klienssel, és a példány élettartamára (egy kérés) megjegyzi.
/// </summary>
public class KeycloakAdminClient(HttpClient http, IOptions<KeycloakAdminOptions> options)
{
    private readonly KeycloakAdminOptions settings = options.Value;
    private string? accessToken;

    /// <summary>
    /// Felhasználók keresése név, e-mail vagy felhasználónév részletére (üres keresésnél az elsők).
    /// </summary>
    public async Task<List<KeycloakUser>> SearchUsersAsync(string? search, int max, CancellationToken ct)
    {
        var path = $"users?briefRepresentation=true&max={max}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            path += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return await GetAsync<List<KeycloakUser>>(path, ct) ?? [];
    }

    /// <summary>
    /// A felhasználó tényleges realm szerepkörei (az összetett szerepkörökön, pl. az alapértelmezetten át kapottakkal együtt).
    /// </summary>
    public async Task<List<string>> GetEffectiveRealmRolesAsync(string userId, CancellationToken ct)
    {
        var roles = await GetAsync<List<KeycloakRole>>($"users/{userId}/role-mappings/realm/composite", ct) ?? [];
        return roles.Select(role => role.Name).ToList();
    }

    /// <summary>
    /// A felhasználó alkalmazás-szerepkörének lecserélése: a többi alkalmazás-szerepkör és az
    /// alapértelmezett szerepkör-csomag lekerül, a megadott szerepkör közvetlenül rákerül.
    /// </summary>
    /// <returns>false, ha a felhasználó nem létezik.</returns>
    public async Task<bool> SetAppRoleAsync(string userId, string role, IReadOnlyCollection<string> appRoles, CancellationToken ct)
    {
        var current = await GetAsync<List<KeycloakRole>>($"users/{userId}/role-mappings/realm", ct);
        if (current is null)
        {
            return false;
        }

        // Az alapértelmezett csomag (default-roles-*) is futó szerepkört ad, ezért azt is levesszük.
        var toRemove = current.Where(r => appRoles.Contains(r.Name) || r.Name.StartsWith("default-roles-", StringComparison.Ordinal)).ToList();
        if (toRemove.Count > 0)
        {
            await WriteAsync(HttpMethod.Delete, $"users/{userId}/role-mappings/realm", toRemove, ct);
        }

        var target = await GetAsync<KeycloakRole>($"roles/{Uri.EscapeDataString(role)}", ct)
            ?? throw new InvalidOperationException($"A '{role}' szerepkör nincs a realmben.");
        return await WriteAsync(HttpMethod.Post, $"users/{userId}/role-mappings/realm", new[] { target }, ct);
    }

    /// <summary>
    /// A felhasználó engedélyezése vagy letiltása (letiltva nem tud bejelentkezni).
    /// </summary>
    /// <returns>false, ha a felhasználó nem létezik.</returns>
    public Task<bool> SetEnabledAsync(string userId, bool enabled, CancellationToken ct) =>
        WriteAsync(HttpMethod.Put, $"users/{userId}", new { enabled }, ct);

    /// <summary>
    /// GET kérés az Admin API-ra; 404-nél null.
    /// </summary>
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    /// <summary>
    /// Módosító kérés az Admin API-ra JSON törzzsel; 404-nél false.
    /// </summary>
    private async Task<bool> WriteAsync(HttpMethod method, string path, object body, CancellationToken ct)
    {
        using var response = await SendAsync(method, path, body, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>
    /// Kérés a realm Admin API-jára a service account tokenjével.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, $"{settings.BaseUrl}/admin/realms/{settings.Realm}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await http.SendAsync(request, ct);
    }

    /// <summary>
    /// Service account token client credentials folyamattal; a példány élettartamára megjegyzi.
    /// </summary>
    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (accessToken is not null)
        {
            return accessToken;
        }

        using var response = await http.PostAsync(
            $"{settings.BaseUrl}/realms/{settings.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
            }),
            ct);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        accessToken = token?.AccessToken ?? throw new InvalidOperationException("A Keycloak nem adott tokent.");
        return accessToken;
    }

    /// <summary>A token végpont válaszának szükséges része.</summary>
    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}
