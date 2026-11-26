using Analytics.Api.Risk;
using Contracts.Demo;
using Integration.Tests.Infrastructure;

namespace Integration.Tests;

/// <summary>
/// Demóadatok eseménylánca (UC18): egy `DemoDataRequested` (éles helyzetben az Identity küldi) hatására a
/// Training legenerálja a 12 hét adatot és kiküldi az edzés-eseményeket, amelyekből az Analytics kiszámolja
/// a kockázatot; a `DemoDataRemoved` után a `RunnerDataReset` törli a futó Analytics-adatait.
/// </summary>
[Collection(ContainerCollection.Name)]
public class DemoDataFlowTests(ContainerFixture containers)
{
    [Fact]
    public async Task DemoDataRequested_MajdRemoved_LetrehozzaMajdTorliAFutoAdatait()
    {
        await using var training = new TrainingApiFactory(containers);
        await using var analytics = new AnalyticsApiFactory(containers);
        await using var publisher = await TestPublisher.StartAsync(containers);

        var runnerId = Guid.NewGuid();
        var runnerClient = analytics.CreateClient().As(runnerId, "runner");

        // A Training 12 hét adatot generál és sok eseményt küld; az Analytics ezeket sorban (egyesével,
        // minden után újraszámolva) dolgozza fel, ezért nagyvonalú a várakozási idő. A teszt a lánc
        // helyességét igazolja (létrejön a kockázat, majd a törlés eltünteti), nem egy konkrét pontszámot.
        var timeout = TimeSpan.FromSeconds(120);

        // Demóadatok kérése erre az egy szimulált futóra (terhelésugrásos minta).
        await publisher.PublishAsync(new DemoDataRequested(
            [new DemoRunner(runnerId, DemoRunnerPattern.SuddenSpike)],
            DateTimeOffset.UtcNow));

        // A lánc átment: a futónak keletkezett kockázati pontszáma.
        var risk = await TestClient.PollUntilAsync(async () =>
        {
            var result = await runnerClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result?.Current;
        }, timeout);
        Assert.NotNull(risk);

        // Demóadatok törlése: a RunnerDataReset törli a futó Analytics-adatait (a kockázat eltűnik).
        await publisher.PublishAsync(new DemoDataRemoved([runnerId], DateTimeOffset.UtcNow));

        var cleared = await TestClient.PollUntilAsync(async () =>
        {
            var result = await runnerClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result is { Current: null } ? result : null;
        }, timeout);
        Assert.NotNull(cleared);
    }
}
