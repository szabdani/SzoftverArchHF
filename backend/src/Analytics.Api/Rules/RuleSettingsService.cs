using Analytics.Api.Persistence;
using Analytics.Api.Risk;

namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások tárolása; mentés után a mai pontszámok azonnal az új küszöbökkel frissülnek.
/// </summary>
public class RuleSettingsService(AnalyticsDbContext db, IRiskService riskService, TimeProvider time) : IRuleSettingsService
{
    /// <summary>
    /// Az aktuális beállítások (ha még nincs mentve, a spec alapértelmezései).
    /// </summary>
    public async Task<RuleSettingsDto> GetAsync(CancellationToken ct) => RuleSettingsDto.From(await db.GetRuleSettingsAsync(ct));

    /// <summary>
    /// A beállítások felülírása; utána minden futó mai pontszáma újraszámolódik.
    /// </summary>
    /// <returns>Az elmentett beállítások.</returns>
    public async Task<RuleSettingsDto> UpdateAsync(RuleSettingsDto request, CancellationToken ct)
    {
        var settings = await db.RuleSettings.FindAsync([RuleSettings.SingletonId], ct);
        if (settings is null)
        {
            settings = new RuleSettings();
            db.RuleSettings.Add(settings);
        }

        request.ApplyTo(settings);
        settings.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);

        await riskService.RecalculateAllAsync(ct);
        return RuleSettingsDto.From(settings);
    }
}
