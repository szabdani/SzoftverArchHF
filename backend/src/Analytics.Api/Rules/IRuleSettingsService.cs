namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások kezelése az adminisztrátornak (UC17).
/// </summary>
public interface IRuleSettingsService
{
    /// <summary>
    /// Az aktuális beállítások (ha még nincs mentve, a spec alapértelmezései).
    /// </summary>
    Task<RuleSettingsDto> GetAsync(CancellationToken ct);

    /// <summary>
    /// A beállítások felülírása; utána minden futó mai pontszáma az új küszöbökkel számolódik újra.
    /// </summary>
    /// <returns>Az elmentett beállítások.</returns>
    Task<RuleSettingsDto> UpdateAsync(RuleSettingsDto request, CancellationToken ct);
}
