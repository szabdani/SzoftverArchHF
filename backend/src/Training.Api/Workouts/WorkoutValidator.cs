using Contracts;

namespace Training.Api.Workouts;

/// <summary>A spec 3.2 táblázatának szabályai. A hibakulcsok a JSON mezőnevek (camelCase).</summary>
public static class WorkoutValidator
{
    public const decimal MinDistanceKm = 0.1m;
    public const decimal MaxDistanceKm = 100m;
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(10);
    public const int MinHeartRate = 40;
    public const int MaxHeartRate = 220;
    public const int MinElevationM = 0;
    public const int MaxElevationM = 5000;
    public const int MinRpe = 1;
    public const int MaxRpe = 10;

    public static bool TryValidate(
        WorkoutRequest request,
        DateOnly today,
        out ValidWorkoutInput input,
        out Dictionary<string, string[]> errors)
    {
        var problems = new Dictionary<string, List<string>>();
        void Fail(string field, string message)
        {
            if (!problems.TryGetValue(field, out var list))
            {
                problems[field] = list = [];
            }

            list.Add(message);
        }

        if (request.Date is not { } date)
        {
            Fail("date", "A dátum megadása kötelező.");
        }
        else if (date > today)
        {
            Fail("date", "A dátum nem lehet jövőbeli.");
        }

        if (request.Type is not { } type)
        {
            Fail("type", "Az edzéstípus megadása kötelező.");
        }
        else if (!Enum.IsDefined(type))
        {
            Fail("type", "Ismeretlen edzéstípus.");
        }

        if (request.DistanceKm is not { } distance)
        {
            Fail("distanceKm", "A táv megadása kötelező.");
        }
        else if (distance < MinDistanceKm || distance > MaxDistanceKm)
        {
            Fail("distanceKm", "A táv 0,1 és 100 km között lehet.");
        }
        else if (decimal.Round(distance, 2) != distance)
        {
            Fail("distanceKm", "A táv legfeljebb két tizedesjegy pontosságú lehet.");
        }

        if (request.Duration is not { } duration)
        {
            Fail("duration", "Az időtartam megadása kötelező.");
        }
        else if (duration < MinDuration || duration > MaxDuration)
        {
            Fail("duration", "Az időtartam 1 perc és 10 óra között lehet.");
        }
        else if (duration.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            Fail("duration", "Az időtartam egész másodpercre adható meg (óó:pp:mm).");
        }

        if (request.AvgHeartRate is < MinHeartRate or > MaxHeartRate)
        {
            Fail("avgHeartRate", "Az átlagpulzus 40 és 220 ütés/perc között lehet.");
        }

        if (request.ElevationGainM is < MinElevationM or > MaxElevationM)
        {
            Fail("elevationGainM", "Az emelkedés 0 és 5000 méter között lehet.");
        }

        if (request.Rpe is not { } rpe)
        {
            Fail("rpe", "Az RPE megadása kötelező.");
        }
        else if (rpe is < MinRpe or > MaxRpe)
        {
            Fail("rpe", "Az RPE 1 és 10 közötti egész szám lehet.");
        }

        if (request.Note is { Length: > Workout.NoteMaxLength })
        {
            Fail("note", "A megjegyzés legfeljebb 500 karakter lehet.");
        }

        errors = problems.ToDictionary(p => p.Key, p => p.Value.ToArray());
        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        input = new ValidWorkoutInput(
            request.Date!.Value,
            request.Type!.Value,
            request.DistanceKm!.Value,
            (int)request.Duration!.Value.TotalSeconds,
            request.AvgHeartRate,
            request.ElevationGainM,
            request.Rpe!.Value,
            request.Note);
        return true;
    }
}
