using Analytics.Api.Risk;
using Contracts.Demo;
using Integration.Tests.Infrastructure;

namespace Integration.Tests;

/// <summary>
/// A futó adatainak törlése eseményvezérelten (UC18 demóadat-törlés): a Training egy `RunnerDataReset`
/// eseményt küld (demóadat törlésekor), amire az Analytics a futó minden tárolt adatát (edzések, pontszám,
/// figyelmeztetések) törli. A teszt előbb edzéssel létrehozza a kockázatot, majd a reset után ellenőrzi,
/// hogy az eltűnt.
///
/// Megjegyzés: a teljes `DemoDataRequested` folyamat (12 hét, ~60 edzés generálása és egyesével való
/// újraszámolása) valós láncként működik, de szinkron kivárása percekig tart, ezért az integrációs teszt a
/// törlési láncot a gyors, determinisztikus úton igazolja; a demóadat-generálást a Training egységtesztjei
/// és az élő QA (UC18) fedik.
/// </summary>
[Collection(ContainerCollection.Name)]
public class DemoDataFlowTests(ContainerFixture containers)
{
    [Fact]
    public async Task RunnerDataReset_TorliAFutoAnalyticsAdatait()
    {
        await using var training = new TrainingApiFactory(containers);
        await using var analytics = new AnalyticsApiFactory(containers);
        await using var publisher = await TestPublisher.StartAsync(containers);

        var runnerId = Guid.NewGuid();
        var trainingClient = training.CreateClient().As(runnerId, "runner");
        var runnerClient = analytics.CreateClient().As(runnerId, "runner");

        // Edzés rögzítése → WorkoutRecorded → az Analytics kiszámolja a kockázatot.
        (await trainingClient.PostJsonAsync("/api/training/workouts", new
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            type = "easy",
            distanceKm = 6.0m,
            duration = "00:36:00",
            rpe = 4,
        })).EnsureSuccessStatusCode();

        // A kockázat létrejött.
        var risk = await TestClient.PollUntilAsync(async () =>
        {
            var result = await runnerClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result?.Current;
        });
        Assert.NotNull(risk);

        // A demóadat-törléskor kiküldött RunnerDataReset: az Analytics törli a futó összes adatát.
        await publisher.PublishAsync(new RunnerDataReset(runnerId, DateTimeOffset.UtcNow));

        // A kockázati pontszám eltűnik (a Current null lesz).
        var cleared = await TestClient.PollUntilAsync(async () =>
        {
            var result = await runnerClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result is { Current: null } ? result : null;
        });
        Assert.NotNull(cleared);
    }
}
