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

        // A terhelésugrásos minta magas kockázatot ad – ezt egyértelmű ellenőrizni.
        var runnerId = Guid.NewGuid();
        var runnerClient = analytics.CreateClient().As(runnerId, "runner");

        // Demóadatok kérése erre az egy szimulált futóra.
        await publisher.PublishAsync(new DemoDataRequested(
            [new DemoRunner(runnerId, DemoRunnerPattern.SuddenSpike)],
            DateTimeOffset.UtcNow));

        // A Training generál → events → Analytics számol: várunk a magas kockázatra.
        var risk = await TestClient.PollUntilAsync(async () =>
        {
            var result = await runnerClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result?.Current is { Category: RiskCategory.High } current ? current : null;
        });
        Assert.NotNull(risk);

        // Demóadatok törlése: a RunnerDataReset törli a futó Analytics-adatait.
        await publisher.PublishAsync(new DemoDataRemoved([runnerId], DateTimeOffset.UtcNow));

        // Várunk, amíg a kockázati pontszám eltűnik (a Current null lesz).
        var cleared = await TestClient.PollUntilAsync(async () =>
        {
            var result = await runnerClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result is { Current: null } ? result : null;
        });
        Assert.NotNull(cleared);
    }
}
