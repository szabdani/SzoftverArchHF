namespace Contracts.Training;

/// <summary>Tervezett edzés állapota (spec 3.4): tervezett, teljesített, kihagyott.</summary>
public enum PlannedWorkoutStatus
{
    Planned,
    Completed,
    Missed,
}

/// <summary>Egy tervezett edzés állapota, ahogy a Training szolgáltatás a mentés után látja.</summary>
public record PlannedWorkoutSnapshot(
    Guid PlannedWorkoutId,
    Guid RunnerId,
    DateOnly Date,
    WorkoutType Type,
    decimal? PlannedDistanceKm,
    int? PlannedDurationSeconds,
    int TargetRpe,
    PlannedWorkoutStatus Status);

/// <summary>Tervezett edzés létrejött vagy megváltozott (adat, dátum vagy állapot).</summary>
public record PlannedWorkoutChanged(PlannedWorkoutSnapshot PlannedWorkout, DateTimeOffset OccurredAt);

/// <summary>Tervezett edzés törölve (önállóan vagy a tervvel együtt).</summary>
public record PlannedWorkoutRemoved(Guid PlannedWorkoutId, Guid RunnerId, DateTimeOffset OccurredAt);
