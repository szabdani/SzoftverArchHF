namespace Analytics.Api.Rules;

/// <summary>
/// A szabálybeállítások ellenőrzése. A hibakulcsok a JSON mezőnevek (camelCase).
/// </summary>
public static class RuleSettingsValidator
{
    /// <summary>
    /// Ellenőrzi a küszöbök tartományát, és hogy az emelkedő ACWR-sáv a kritikus alatt kezdődjön.
    /// </summary>
    public static bool TryValidate(RuleSettingsDto request, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (request.AcwrCritical is < 0.5 or > 5)
        {
            errors["acwrCritical"] = ["A kritikus ACWR 0,5 és 5 között lehet."];
        }

        if (request.AcwrElevated is < 0.5 or > 5)
        {
            errors["acwrElevated"] = ["Az emelkedő ACWR 0,5 és 5 között lehet."];
        }
        else if (request.AcwrElevated >= request.AcwrCritical)
        {
            errors["acwrElevated"] = ["Az emelkedő ACWR küszöbnek kisebbnek kell lennie a kritikusnál."];
        }

        if (request.WeeklyKmIncreasePercent is < 1 or > 100)
        {
            errors["weeklyKmIncreasePercent"] = ["A heti km-növekedés küszöbe 1 és 100% között lehet."];
        }

        if (request.IntenseRpe is < 1 or > 10)
        {
            errors["intenseRpe"] = ["Az intenzív RPE 1 és 10 között lehet."];
        }

        if (request.IntenseDaysPerWeek is < 2 or > 7)
        {
            errors["intenseDaysPerWeek"] = ["A heti intenzív napok küszöbe 2 és 7 között lehet."];
        }

        if (request.MissedWorkoutsPerWeek is < 1 or > 7)
        {
            errors["missedWorkoutsPerWeek"] = ["A heti kihagyott edzések küszöbe 1 és 7 között lehet."];
        }

        return errors.Count == 0;
    }
}
