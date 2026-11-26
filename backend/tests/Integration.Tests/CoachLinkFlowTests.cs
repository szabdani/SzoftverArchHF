using System.Net.Http.Json;
using Analytics.Api.Risk;
using Contracts.Identity;
using Integration.Tests.Infrastructure;

namespace Integration.Tests;

/// <summary>
/// Edző–futó kapcsolat eseménylánca (UC12–UC14): egy `CoachRunnerLinked` esemény (éles helyzetben az
/// Identity küldi) után az Analytics felveszi a kapcsolatot a saját másolatába, és az edző szerveroldalon
/// hozzáfér a hozzárendelt futó kockázatához; a kapcsolat előtt viszont 403-at kap.
/// </summary>
[Collection(ContainerCollection.Name)]
public class CoachLinkFlowTests(ContainerFixture containers)
{
    [Fact]
    public async Task CoachRunnerLinked_UtanAzEdzoLatjaAFutoKockazatat()
    {
        await using var training = new TrainingApiFactory(containers);
        await using var analytics = new AnalyticsApiFactory(containers);
        await using var publisher = await TestPublisher.StartAsync(containers);

        var runnerId = Guid.NewGuid();
        var coachId = Guid.NewGuid();

        // A futó rögzít egy edzést, hogy legyen mit megnéznie az edzőnek.
        var runnerClient = training.CreateClient().As(runnerId, "runner");
        (await runnerClient.PostJsonAsync("/api/training/workouts", new
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            type = "easy",
            distanceKm = 6.0m,
            duration = "00:36:00",
            rpe = 4,
        })).EnsureSuccessStatusCode();

        var coachClient = analytics.CreateClient().As(coachId, "coach");

        // Kapcsolat előtt az edző nem fér hozzá a futó adataihoz (nincs CoachRunnerLink).
        using (var denied = await coachClient.GetAsync($"/api/analytics/risk?runnerId={runnerId}"))
        {
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, denied.StatusCode);
        }

        // Az Identity által küldött esemény szimulálása: a kapcsolat létrejön.
        await publisher.PublishAsync(new CoachRunnerLinked(coachId, runnerId, DateTimeOffset.UtcNow));

        // A kapcsolat feldolgozása (Analytics CoachRunnerLinkConsumer) aszinkron: várunk, amíg az edző hozzáfér.
        var risk = await TestClient.PollUntilAsync(async () =>
        {
            using var response = await coachClient.GetAsync($"/api/analytics/risk?runnerId={runnerId}");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<RiskResponse>();
        });

        Assert.NotNull(risk);
    }
}
