using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Validation;
using Contracts;

namespace Training.Api.Workouts;

/// <summary>
/// Létrehozás és módosítás kérése (spec 3.2). A mezőnkénti szabályok az attribútumokban vannak: a szerver
/// ezekkel validál, és ugyanezek kerülnek az OpenAPI sémába a frontendnek. A kötelező mezők is nullable-k,
/// hogy a hiányzó mezőt érthető hibával utasíthassuk el. Az időtartam formátuma óó:pp:mm (pl. "01:05:30");
/// a táv két tizedesre kerekítődik. A tervezett edzés azonosítója (UC08) csak rögzítéskor számít.
/// </summary>
public record WorkoutRequest(
    [property: Required(ErrorMessage = "A dátum megadása kötelező.")]
    DateOnly? Date,
    [property: Required(ErrorMessage = "Az edzéstípus megadása kötelező.")]
    [property: EnumDataType(typeof(WorkoutType), ErrorMessage = "Ismeretlen edzéstípus.")]
    WorkoutType? Type,
    [property: Required(ErrorMessage = "A táv megadása kötelező.")]
    [property: Range(0.1, 100, ErrorMessage = "A táv 0,1 és 100 km között lehet.")]
    decimal? DistanceKm,
    [property: Required(ErrorMessage = "Az időtartam megadása kötelező.")]
    [property: RegularExpression(ValidationPatterns.Duration1MinTo10Hours, ErrorMessage = "Az időtartam 1 perc és 10 óra között lehet, óó:pp:mm formátumban.")]
    TimeSpan? Duration,
    [property: Range(40, 220, ErrorMessage = "Az átlagpulzus 40 és 220 ütés/perc között lehet.")]
    int? AvgHeartRate,
    [property: Range(0, 5000, ErrorMessage = "Az emelkedés 0 és 5000 méter között lehet.")]
    int? ElevationGainM,
    [property: Required(ErrorMessage = "Az RPE megadása kötelező.")]
    [property: Range(1, 10, ErrorMessage = "Az RPE 1 és 10 közötti egész szám lehet.")]
    int? Rpe,
    [property: MaxLength(Workout.NoteMaxLength, ErrorMessage = "A megjegyzés legfeljebb 500 karakter lehet.")]
    string? Note,
    Guid? PlannedWorkoutId = null);

/// <summary>A validáció után már biztosan teljes, érvényes bemenet.</summary>
public record ValidWorkoutInput(
    DateOnly Date,
    WorkoutType Type,
    decimal DistanceKm,
    int DurationSeconds,
    int? AvgHeartRate,
    int? ElevationGainM,
    int Rpe,
    string? Note,
    Guid? PlannedWorkoutId = null);

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
    DateTimeOffset UpdatedAt,
    Guid? PlannedWorkoutId)
{
    /// <summary>Válasz-DTO az entitásból; az időtartamot TimeSpan-ként (óó:pp:mm) adja vissza.</summary>
    public static WorkoutResponse From(Workout w) => new(
        w.Id, w.RunnerId, w.Date, w.Type, w.DistanceKm, TimeSpan.FromSeconds(w.DurationSeconds),
        w.PaceSecondsPerKm, w.AvgHeartRate, w.ElevationGainM, w.Rpe, w.Note, w.Load, w.CreatedAt, w.UpdatedAt,
        w.PlannedWorkoutId);
}