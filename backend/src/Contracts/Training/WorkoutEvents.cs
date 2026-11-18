namespace Contracts.Training;

/// <summary>Egy edzés teljes állapota, ahogy a Training szolgáltatás a mentés után látja.</summary>
public record WorkoutSnapshot(
    Guid WorkoutId,
    Guid RunnerId,
    DateOnly Date,
    WorkoutType Type,
    decimal DistanceKm,
    int DurationSeconds,
    int Rpe,
    int? AvgHeartRate,
    int? ElevationGainM,
    double Load);

public record WorkoutRecorded(WorkoutSnapshot Workout, DateTimeOffset OccurredAt);

public record WorkoutUpdated(WorkoutSnapshot Workout, DateTimeOffset OccurredAt);

public record WorkoutDeleted(Guid WorkoutId, Guid RunnerId, DateOnly Date, DateTimeOffset OccurredAt);
