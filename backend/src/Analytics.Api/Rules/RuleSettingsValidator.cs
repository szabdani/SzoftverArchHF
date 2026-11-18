using BuildingBlocks.Validation;

namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások ellenőrzése. A tartományok a <see cref="RuleSettingsDto"/> attribútumaiban vannak
/// (közös séma a frontenddel); itt csak a két mezőt összevető szabály van.
/// </summary>
public static class RuleSettingsValidator
{
    /// <summary>
    /// Ellenőrzi a tartományokat, és hogy az emelkedő ACWR-sáv a kritikus alatt kezdődjön.
    /// </summary>
    public static bool TryValidate(RuleSettingsDto request, out Dictionary<string, string[]> errors)
    {
        errors = RequestValidation.ValidateAnnotations(request);

        if (request.AcwrElevated >= request.AcwrCritical)
        {
            errors.TryAdd("acwrElevated", ["Az emelkedő ACWR küszöbnek kisebbnek kell lennie a kritikusnál."]);
        }

        return errors.Count == 0;
    }
}
