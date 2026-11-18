using Contracts;
using Contracts.Training;

namespace Analytics.Api.ReadModels;

/// <summary>
/// Egy tervezett edzés helyi másolata a Training szolgáltatás eseményeiből (a kihagyott
/// edzések számolásához, később a javaslathoz).
/// </summary>
public class PlannedWorkoutRecord
{
    public Guid Id { get; set; }
    public Guid RunnerId { get; set; }
    public DateOnly Date { get; set; }
    public WorkoutType Type { get; set; }
    public decimal? PlannedDistanceKm { get; set; }
    public int? PlannedDurationSeconds { get; set; }
    public int TargetRpe { get; set; }
    public PlannedWorkoutStatus Status { get; set; }
}
