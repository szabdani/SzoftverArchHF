namespace Analytics.Api.Risk;

/// <summary>
/// Napi ellenőrzés (UC19, az Analytics szolgáltatás része): minden futónak kiszámolja a napi
/// kockázati pontszámát akkor is, ha aznap nem volt edzése. Induláskor és utána óránként fut,
/// de csak azoknak számol, akiknek a mai pontszáma még hiányzik, így naponta egyszer.
/// </summary>
public class DailyRiskJob(IServiceScopeFactory scopeFactory, TimeProvider time, ILogger<DailyRiskJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    /// <summary>
    /// A háttérfolyamat: azonnal egy ellenőrzés, utána óránként egy, a leállításig.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Egy ellenőrzés saját DI scope-ban; a hibát naplózza, hogy a következő futás ettől még elinduljon.
    /// </summary>
    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IRiskService>();
            var count = await service.RecalculateMissingAsync(ct);
            if (count > 0)
            {
                logger.LogInformation("{Count} futó napi kockázati pontszáma kiszámolva.", count);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "A napi kockázati pontszám számítása nem sikerült.");
        }
    }
}
