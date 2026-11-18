using Training.Api.Workouts;

namespace Training.Api.Plans;

/// <summary>
/// A terv és a tervezett edzés kérésének ellenőrzése. A hibakulcsok a JSON mezőnevek (camelCase).
/// A táv, időtartam és RPE határai ugyanazok, mint a rögzített edzésnél (<see cref="WorkoutValidator"/>).
/// </summary>
public static class PlanValidator
{
    public const int MaxPlanLengthDays = 366;

    /// <summary>
    /// Ellenőrzi a terv kérését: kötelező név, kezdő- és záródátum, a záródátum nem korábbi,
    /// a terv legfeljebb egy év.
    /// </summary>
    public static bool TryValidatePlan(PlanRequest request, out ValidPlanInput input, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();
        var name = request.Name?.Trim() ?? "";

        if (name.Length == 0)
        {
            errors["name"] = ["A terv nevének megadása kötelező."];
        }
        else if (name.Length > TrainingPlan.NameMaxLength)
        {
            errors["name"] = ["A terv neve legfeljebb 100 karakter lehet."];
        }

        if (request.StartDate is null)
        {
            errors["startDate"] = ["A kezdődátum megadása kötelező."];
        }

        if (request.EndDate is null)
        {
            errors["endDate"] = ["A záródátum megadása kötelező."];
        }
        else if (request.StartDate is { } start && request.EndDate is { } end)
        {
            if (end < start)
            {
                errors["endDate"] = ["A záródátum nem lehet korábbi a kezdődátumnál."];
            }
            else if (end.DayNumber - start.DayNumber > MaxPlanLengthDays)
            {
                errors["endDate"] = ["Egy terv legfeljebb egy évre szólhat."];
            }
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        input = new ValidPlanInput(name, request.StartDate!.Value, request.EndDate!.Value, request.LinkActiveGoal);
        return true;
    }

    /// <summary>
    /// Ellenőrzi a tervezett edzés kérését. A dátumnak a terv időszakába kell esnie; a tervezett
    /// táv és időtartam közül legalább az egyik kell.
    /// </summary>
    public static bool TryValidatePlannedWorkout(
        PlannedWorkoutRequest request,
        DateOnly planStart,
        DateOnly planEnd,
        out ValidPlannedWorkoutInput input,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (request.Date is not { } date)
        {
            errors["date"] = ["A dátum megadása kötelező."];
        }
        else if (date < planStart || date > planEnd)
        {
            errors["date"] = [$"A dátumnak a terv időszakába kell esnie ({planStart:yyyy.MM.dd.} – {planEnd:yyyy.MM.dd.})."];
        }

        if (request.Type is not { } type)
        {
            errors["type"] = ["Az edzéstípus megadása kötelező."];
        }
        else if (!Enum.IsDefined(type))
        {
            errors["type"] = ["Ismeretlen edzéstípus."];
        }

        if (request.PlannedDistanceKm is null && request.PlannedDuration is null)
        {
            errors["plannedDistanceKm"] = ["A tervezett táv vagy időtartam közül legalább az egyiket meg kell adni."];
        }

        if (request.PlannedDistanceKm is { } distance
            && (distance < WorkoutValidator.MinDistanceKm || distance > WorkoutValidator.MaxDistanceKm || decimal.Round(distance, 2) != distance))
        {
            errors["plannedDistanceKm"] = ["A táv 0,1 és 100 km között lehet, legfeljebb két tizedesjeggyel."];
        }

        if (request.PlannedDuration is { } duration
            && (duration < WorkoutValidator.MinDuration || duration > WorkoutValidator.MaxDuration || duration.Ticks % TimeSpan.TicksPerSecond != 0))
        {
            errors["plannedDuration"] = ["Az időtartam 1 perc és 10 óra között lehet, egész másodpercre (óó:pp:mm)."];
        }

        if (request.TargetRpe is not { } rpe)
        {
            errors["targetRpe"] = ["A cél RPE megadása kötelező."];
        }
        else if (rpe is < WorkoutValidator.MinRpe or > WorkoutValidator.MaxRpe)
        {
            errors["targetRpe"] = ["A cél RPE 1 és 10 közötti egész szám lehet."];
        }

        if (request.Note is { Length: > PlannedWorkout.NoteMaxLength })
        {
            errors["note"] = ["A megjegyzés legfeljebb 500 karakter lehet."];
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        input = new ValidPlannedWorkoutInput(
            request.Date!.Value,
            request.Type!.Value,
            request.PlannedDistanceKm,
            request.PlannedDuration is { } plannedDuration ? (int)plannedDuration.TotalSeconds : null,
            request.TargetRpe!.Value,
            request.Note);
        return true;
    }
}
