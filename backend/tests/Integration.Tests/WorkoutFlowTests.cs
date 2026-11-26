using Analytics.Api.Risk;
using Integration.Tests.Infrastructure;

namespace Integration.Tests;

/// <summary>
/// Edzés mentése a Training API-n → `WorkoutRecorded` esemény → az Analytics `TrainingEventsConsumer`
/// felveszi és újraszámolja a kockázatot. Végpont-végpont ellenőrzés valódi PostgreSQL + RabbitMQ ellen
/// (UC02, UC20): a két szolgáltatás külön adatbázison, de közös brókeren kommunikál.
/// </summary>
[Collection(ContainerCollection.Name)]
public class WorkoutFlowTests(ContainerFixture containers)
{
    [Fact]
    public async Task EdzesMentese_UtanAzAnalyticsUjraszamoljaAKockazatot()
    {
        await using var training = new TrainingApiFactory(containers);
        await using var analytics = new AnalyticsApiFactory(containers);
        var runnerId = Guid.NewGuid();

        var trainingClient = training.CreateClient().As(runnerId, "runner");
        var analyticsClient = analytics.CreateClient().As(runnerId, "runner");

        // Edzés rögzítése a futó nevében.
        var response = await trainingClient.PostJsonAsync("/api/training/workouts", new
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            type = "easy",
            distanceKm = 7.0m,
            duration = "00:42:00",
            rpe = 5,
        });
        response.EnsureSuccessStatusCode();

        // Az Analytics aszinkron dolgozza fel az eseményt: a futó kockázati pontszámára várunk.
        var risk = await TestClient.PollUntilAsync(async () =>
        {
            var result = await analyticsClient.TryGetAsync<RiskResponse>($"/api/analytics/risk?runnerId={runnerId}");
            return result?.Current;
        });

        // Egyetlen könnyű edzés: a lánc végigfutott (jött snapshot), és a pontszám alacsony (egyetlen
        // könnyű edzésből nem keletkezik túlterhelési kockázat).
        Assert.NotNull(risk);
        Assert.Equal(RiskCategory.Low, risk.Category);
        Assert.InRange(risk.Score, 0, 29);
    }
}
