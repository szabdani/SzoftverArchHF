using BuildingBlocks.Validation;

namespace Training.Api.Workouts;

/// <summary>
/// Az edzés kérésének ellenőrzése (spec 3.2). A mezőnkénti szabályok a <see cref="WorkoutRequest"/>
/// attribútumaiban vannak (közös séma a frontenddel); itt csak a mai naptól függő szabály van.
/// </summary>
public static class WorkoutValidator
{
    /// <summary>
    /// Ellenőrzi a kérést. Siker esetén a teljes, érvényes bemenetet adja vissza (a táv két tizedesre
    /// kerekítve), hiba esetén mezőnként a magyar hibaüzeneteket.
    /// </summary>
    public static bool TryValidate(
        WorkoutRequest request,
        DateOnly today,
        out ValidWorkoutInput input,
        out Dictionary<string, string[]> errors)
    {
        errors = RequestValidation.ValidateAnnotations(request);

        // Szerveroldali szabály: a dátum a felhasználó mai napjához képest nem lehet jövőbeli.
        if (request.Date > today)
        {
            errors.TryAdd("date", ["A dátum nem lehet jövőbeli."]);
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        input = new ValidWorkoutInput(
            request.Date!.Value,
            request.Type!.Value,
            decimal.Round(request.DistanceKm!.Value, 2, MidpointRounding.AwayFromZero),
            (int)request.Duration!.Value.TotalSeconds,
            request.AvgHeartRate,
            request.ElevationGainM,
            request.Rpe!.Value,
            request.Note,
            request.PlannedWorkoutId);
        return true;
    }
}
