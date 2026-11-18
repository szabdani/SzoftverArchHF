using BuildingBlocks.Validation;
using Contracts;

namespace Training.Api.Goals;

/// <summary>
/// A cél kérésének ellenőrzése. A mezőnkénti szabályok a <see cref="GoalRequest"/> attribútumaiban vannak
/// (közös séma a frontenddel); itt csak a mai naptól és a céltípustól függő szabályok vannak.
/// </summary>
public static class GoalValidator
{
    /// <summary>
    /// Ellenőrzi a kérést. Siker esetén a teljes, érvényes bemenetet adja vissza (a fix típusoknál
    /// a szabványos távval), hiba esetén mezőnként a magyar hibaüzenetet.
    /// </summary>
    public static bool TryValidate(
        GoalRequest request,
        DateOnly today,
        out ValidGoalInput input,
        out Dictionary<string, string[]> errors)
    {
        errors = RequestValidation.ValidateAnnotations(request);

        // Szerveroldali szabályok: egyéni célnál kötelező a táv, és a céldátum nem lehet múltbeli.
        if (request.Type == GoalType.Custom && request.DistanceKm is null)
        {
            errors.TryAdd("distanceKm", ["Egyéni célnál a táv megadása kötelező."]);
        }

        if (request.TargetDate < today)
        {
            errors.TryAdd("targetDate", ["A céldátum nem lehet múltbeli."]);
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        var goalType = request.Type!.Value;
        var customDistance = decimal.Round(request.DistanceKm ?? 0, 2, MidpointRounding.AwayFromZero);
        input = new ValidGoalInput(
            goalType,
            Goal.DistanceFor(goalType, customDistance),
            request.TargetDate!.Value,
            request.TargetTime is { } time ? (int)time.TotalSeconds : null);
        return true;
    }
}
