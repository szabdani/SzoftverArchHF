using Contracts;

namespace Training.Api.Workouts;

/// <summary>
/// Létrehozás és módosítás kérése. A kötelező mezők is nullable-k, hogy a hiányzó mezőt
/// érthető validációs hibával utasíthassuk el, ne csendes alapértékkel.
/// Az időtartam formátuma óó:pp:mm (pl. "01:05:30").
/// </summary>
public record WorkoutRequest(
    DateOnly? Date,
    WorkoutType? Type,
    decimal? DistanceKm,
    TimeSpan? Duration,
    int? AvgHeartRate,
    int? ElevationGainM,
    int? Rpe,
    string? Note);

/// <summary>A validáció után már biztosan teljes, érvényes bemenet.</summary>
public record ValidWorkoutInput(
    DateOnly Date,
    WorkoutType Type,
    decimal DistanceKm,
    int DurationSeconds,
    int? AvgHeartRate,
    int? ElevationGainM,
    int Rpe,
    string? Note);

/// <summary>Egy edzés a kliens felé, számított tempóval és terheléssel.</summary>
public record WorkoutResponse(
    Guid Id,
    Guid RunnerId,
    DateOnly Date,
    WorkoutType Type,
    decimal DistanceKm,
    TimeSpan Duration,
    int PaceSecondsPerKm,
    int? AvgHeartRate,
    int? ElevationGainM,
    int Rpe,
    string? Note,
    double Load,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Válasz-DTO az entitásból; az időtartamot TimeSpan-ként (óó:pp:mm) adja vissza.</summary>
    public static WorkoutResponse From(Workout w) => new(
        w.Id, w.RunnerId, w.Date, w.Type, w.DistanceKm, TimeSpan.FromSeconds(w.DurationSeconds),
        w.PaceSecondsPerKm, w.AvgHeartRate, w.ElevationGainM, w.Rpe, w.Note, w.Load, w.CreatedAt, w.UpdatedAt);
}