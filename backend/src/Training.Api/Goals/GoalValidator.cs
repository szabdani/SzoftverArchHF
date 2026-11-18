using Contracts;
namespace Training.Api.Goals;

/// <summary>
/// A cél kérésének ellenőrzése. A hibakulcsok a JSON mezőnevek (camelCase).
/// </summary>
public static class GoalValidator
{
    public const decimal MinCustomDistanceKm = 0.1m;
    public const decimal MaxCustomDistanceKm = 500m;
    public static readonly TimeSpan MinTargetTime = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxTargetTime = TimeSpan.FromHours(24);

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
        errors = new Dictionary<string, string[]>();

        if (request.Type is not { } type)
        {
            errors["type"] = ["A céltípus megadása kötelező."];
        }
        else if (!Enum.IsDefined(type))
        {
            errors["type"] = ["Ismeretlen céltípus."];
        }
        else if (type == GoalType.Custom)
        {
            // Csak egyéni célnál kell táv; a fix típusoknál a megadott távot figyelmen kívül hagyjuk.
            if (request.DistanceKm is not { } distance)
            {
                errors["distanceKm"] = ["Egyéni célnál a táv megadása kötelező."];
            }
            else if (distance < MinCustomDistanceKm || distance > MaxCustomDistanceKm)
            {
                errors["distanceKm"] = ["A táv 0,1 és 500 km között lehet."];
            }
            else if (decimal.Round(distance, 2) != distance)
            {
                errors["distanceKm"] = ["A táv legfeljebb két tizedesjegy pontosságú lehet."];
            }
        }

        if (request.TargetDate is not { } targetDate)
        {
            errors["targetDate"] = ["A céldátum megadása kötelező."];
        }
        else if (targetDate < today)
        {
            errors["targetDate"] = ["A céldátum nem lehet múltbeli."];
        }

        if (request.TargetTime is { } targetTime)
        {
            if (targetTime < MinTargetTime || targetTime > MaxTargetTime)
            {
                errors["targetTime"] = ["A célidő 1 perc és 24 óra között lehet."];
            }
            else if (targetTime.Ticks % TimeSpan.TicksPerSecond != 0)
            {
                errors["targetTime"] = ["A célidő egész másodpercre adható meg (óó:pp:mm)."];
            }
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        var goalType = request.Type!.Value;
        input = new ValidGoalInput(
            goalType,
            Goal.DistanceFor(goalType, request.DistanceKm ?? 0),
            request.TargetDate!.Value,
            request.TargetTime is { } time ? (int)time.TotalSeconds : null);
        return true;
    }
}
