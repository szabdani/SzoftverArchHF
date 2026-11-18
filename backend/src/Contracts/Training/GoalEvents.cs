namespace Contracts.Training;

/// <summary>
/// A futó aktív célja megváltozott (új cél vagy módosítás). A javaslat a cél típusából dolgozik (spec 3.7).
/// </summary>
public record RunnerGoalChanged(Guid RunnerId, GoalType Type, decimal DistanceKm, DateOnly TargetDate, DateTimeOffset OccurredAt);
