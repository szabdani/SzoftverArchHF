using BuildingBlocks.Time;

namespace Training.Api.Plans;

/// <summary>
/// Ütemezett ellenőrzés (UC19, a Training szolgáltatás része): a napja végéig nem teljesült tervezett
/// edzéseket kihagyottra állítja. Induláskor és utána óránként fut, így a nap vége után legfeljebb
/// egy órával minden lejárt edzés kihagyott lesz; az ismételt futás nem változtat semmin.
/// </summary>
public class MissedWorkoutJob(IServiceScopeFactory scopeFactory, TimeProvider time, ILogger<MissedWorkoutJob> logger)
    : BackgroundService
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
            var service = scope.ServiceProvider.GetRequiredService<IPlanService>();
            var count = await service.MarkMissedAsync(time.GetLocalToday(), ct);
            if (count > 0)
            {
                logger.LogInformation("{Count} tervezett edzés kihagyottra állítva.", count);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "A kihagyott edzések ellenőrzése nem sikerült.");
        }
    }
}
