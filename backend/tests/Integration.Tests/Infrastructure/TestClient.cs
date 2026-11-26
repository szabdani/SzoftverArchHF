using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Segédfüggvények az integrációs tesztek HTTP-hívásaihoz: a teszt-hitelesítés fejléceinek beállítása és
/// az aszinkron (eseményvezérelt) feldolgozásra való várakozás.
/// </summary>
public static class TestClient
{
    /// <summary>Egy adott szerepkörű és azonosítójú felhasználóként hitelesített HTTP-kliens.</summary>
    public static HttpClient As(this HttpClient client, Guid userId, string role)
    {
        client.DefaultRequestHeaders.Remove("X-Test-Sub");
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Sub", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    /// <summary>
    /// Addig próbálja a műveletet, amíg igaz eredményt nem ad, vagy le nem jár az idő. Az eseményvezérelt
    /// feldolgozás (busz + outbox) aszinkron, ezért a célállapotra várni kell, nem feltételezni az azonnaliságot.
    /// </summary>
    public static async Task<T> PollUntilAsync<T>(Func<Task<T?>> probe, TimeSpan? timeout = null)
        where T : class
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        T? last = null;
        while (DateTime.UtcNow < deadline)
        {
            last = await probe();
            if (last is not null)
            {
                return last;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException("A várt állapot nem állt elő a megadott időn belül.");
    }

    /// <summary>Egy GET-kérés eredménye adott típusra, vagy null, ha a válasz nem sikeres (pl. még nincs adat).</summary>
    public static async Task<T?> TryGetAsync<T>(this HttpClient client, string url)
        where T : class
    {
        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    /// <summary>Egy JSON kérést POST-ol és visszaadja a nyers választ (a hívó ellenőrzi a státuszt).</summary>
    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body) =>
        client.PostAsync(url, JsonContent.Create(body));
}
