using Contracts;
using Contracts.Training;

namespace Training.Api.Plans;

/// <summary>
/// Terv létrehozása vagy módosítása. A futó azonosítója csak létrehozáskor számít: az edző ezzel
/// adja meg, melyik futójának készíti; a futó elhagyhatja. Ha <c>LinkActiveGoal</c> igaz,
/// a terv a futó aktuális aktív céljához kapcsolódik.
/// </summary>
public record PlanRequest(Guid? RunnerId, string? Name, DateOnly? StartDate, DateOnly? EndDate, bool LinkActiveGoal);

/// <summary>A validáció után már biztosan teljes, érvényes bemenet.</summary>
public record ValidPlanInput(string Name, DateOnly StartDate, DateOnly EndDate, bool LinkActiveGoal);

/// <summary>Egy terv a kliens felé, a tervezett edzéseivel dátum szerint rendezve.</summary>
public record PlanResponse(
    Guid Id,
    Guid RunnerId,
    Guid CreatedById,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GoalId,
    DateTimeOffset CreatedAt,
    List<PlannedWorkoutResponse> Workouts)
{
    /// <summary>
    /// Válasz-DTO az entitásból (a tervezett edzéseknek betöltve kell lenniük).
    /// </summary>
    public static PlanResponse From(TrainingPlan p) => new(
        p.Id, p.RunnerId, p.CreatedById, p.Name, p.StartDate, p.EndDate, p.GoalId, p.CreatedAt,
        p.Workouts.OrderBy(w => w.Date).Select(PlannedWorkoutResponse.From).ToList());
}

/// <summary>
/// Tervezett edzés létrehozása, módosítása vagy áthelyezése. Tervezett táv vagy időtartam
/// közül legalább az egyik kell. Az időtartam formátuma óó:pp:mm.
/// </summary>
public record PlannedWorkoutRequest(
    DateOnly? Date,
    WorkoutType? Type,
    decimal? PlannedDistanceKm,
    TimeSpan? PlannedDuration,
    int? TargetRpe,
    string? Note);

/// <summary>A validáció után már biztosan teljes, érvényes bemenet.</summary>
public record ValidPlannedWorkoutInput(
    DateOnly Date,
    WorkoutType Type,
    decimal? PlannedDistanceKm,
    int? PlannedDurationSeconds,
    int TargetRpe,
    string? Note);

/// <summary>Egy tervezett edzés a kliens felé (naptár, terv nézet).</summary>
public record PlannedWorkoutResponse(
    Guid Id,
    Guid PlanId,
    Guid RunnerId,
    DateOnly Date,
    WorkoutType Type,
    decimal? PlannedDistanceKm,
    TimeSpan? PlannedDuration,
    int TargetRpe,
    string? Note,
    PlannedWorkoutStatus Status,
    Guid? WorkoutId)
{
    /// <summary>
    /// Válasz-DTO az entitásból; az időtartamot TimeSpan-ként (óó:pp:mm) adja vissza.
    /// </summary>
    public static PlannedWorkoutResponse From(PlannedWorkout w) => new(
        w.Id, w.PlanId, w.RunnerId, w.Date, w.Type, w.PlannedDistanceKm,
        w.PlannedDurationSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
        w.TargetRpe, w.Note, w.Status, w.WorkoutId);
}
