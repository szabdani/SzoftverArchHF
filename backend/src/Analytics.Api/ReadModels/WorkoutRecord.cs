using Contracts;

namespace Analytics.Api.ReadModels;

/// <summary>
/// Egy rögzített edzés helyi másolata a Training szolgáltatás eseményeiből; csak a terhelés
/// számításához szükséges mezőkkel.
/// </summary>
public class WorkoutRecord
{
    public Guid Id { get; set; }
    public Guid RunnerId { get; set; }
    public DateOnly Date { get; set; }
    public WorkoutType Type { get; set; }
    public decimal DistanceKm { get; set; }
    public int DurationSeconds { get; set; }
    public int Rpe { get; set; }
    public int? ElevationGainM { get; set; }

    /// <summary>Edzésterhelés (sRPE), ahogy a Training szolgáltatás mentéskor kiszámolta.</summary>
    public double Load { get; set; }
}
