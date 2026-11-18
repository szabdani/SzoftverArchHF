using Contracts;

namespace Analytics.Api.ReadModels;

/// <summary>
/// A futó aktív céljának helyi másolata a Training eseményeiből; a javaslathoz kell (spec 3.7).
/// </summary>
public class ActiveGoalRecord
{
    public Guid RunnerId { get; set; }
    public GoalType Type { get; set; }
    public decimal DistanceKm { get; set; }
    public DateOnly TargetDate { get; set; }
}
