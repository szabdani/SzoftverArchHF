using BuildingBlocks.Validation;

namespace Training.Api.Plans;

/// <summary>
/// A terv és a tervezett edzés kérésének ellenőrzése. A mezőnkénti szabályok a kérések attribútumaiban
/// vannak (közös séma a frontenddel); itt csak a több mezőtől és a terv időszakától függő szabályok vannak.
/// </summary>
public static class PlanValidator
{
    public const int MaxPlanLengthDays = 366;

    /// <summary>
    /// Ellenőrzi a terv kérését; szerveroldalon: a záródátum nem korábbi, és a terv legfeljebb egy év.
    /// </summary>
    public static bool TryValidatePlan(PlanRequest request, out ValidPlanInput input, out Dictionary<string, string[]> errors)
    {
        errors = RequestValidation.ValidateAnnotations(request);

        if (request.StartDate is { } start && request.EndDate is { } end)
        {
            if (end < start)
            {
                errors.TryAdd("endDate", ["A záródátum nem lehet korábbi a kezdődátumnál."]);
            }
            else if (end.DayNumber - start.DayNumber > MaxPlanLengthDays)
            {
                errors.TryAdd("endDate", ["Egy terv legfeljebb egy évre szólhat."]);
            }
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        input = new ValidPlanInput(request.Name!.Trim(), request.StartDate!.Value, request.EndDate!.Value, request.LinkActiveGoal);
        return true;
    }

    /// <summary>
    /// Ellenőrzi a tervezett edzés kérését; szerveroldalon: a dátum a terv időszakába esik, és a tervezett
    /// táv és időtartam közül legalább az egyik meg van adva.
    /// </summary>
    public static bool TryValidatePlannedWorkout(
        PlannedWorkoutRequest request,
        DateOnly planStart,
        DateOnly planEnd,
        out ValidPlannedWorkoutInput input,
        out Dictionary<string, string[]> errors)
    {
        errors = RequestValidation.ValidateAnnotations(request);

        if (request.Date is { } date && (date < planStart || date > planEnd))
        {
            errors.TryAdd("date", [$"A dátumnak a terv időszakába kell esnie ({planStart:yyyy.MM.dd.} – {planEnd:yyyy.MM.dd.})."]);
        }

        if (request.PlannedDistanceKm is null && request.PlannedDuration is null)
        {
            errors.TryAdd("plannedDistanceKm", ["A tervezett táv vagy időtartam közül legalább az egyiket meg kell adni."]);
        }

        if (errors.Count > 0)
        {
            input = null!;
            return false;
        }

        input = new ValidPlannedWorkoutInput(
            request.Date!.Value,
            request.Type!.Value,
            request.PlannedDistanceKm is { } distance ? decimal.Round(distance, 2, MidpointRounding.AwayFromZero) : null,
            request.PlannedDuration is { } duration ? (int)duration.TotalSeconds : null,
            request.TargetRpe!.Value,
            request.Note);
        return true;
    }
}
