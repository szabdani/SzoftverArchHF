using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Validation;
using Contracts;
using Training.Api.Workouts;

namespace Training.Api.Goals;

/// <summary>
/// Cél kitűzése vagy módosítása. A mezőnkénti szabályok az attribútumokban vannak (közös séma a
/// frontenddel). A táv csak egyéni (custom) célnál kell, a célidő formátuma óó:pp:mm (pl. "01:45:00").
/// </summary>
public record GoalRequest(
    [property: Required(ErrorMessage = "A céltípus megadása kötelező.")]
    [property: EnumDataType(typeof(GoalType), ErrorMessage = "Ismeretlen céltípus.")]
    GoalType? Type,
    [property: Range(0.1, 500, ErrorMessage = "A táv 0,1 és 500 km között lehet.")]
    decimal? DistanceKm,
    [property: Required(ErrorMessage = "A céldátum megadása kötelező.")]
    DateOnly? TargetDate,
    [property: RegularExpression(ValidationPatterns.Duration1MinTo10Hours, ErrorMessage = "A célidő 1 perc és 10 óra között lehet, óó:pp:mm formátumban.")]
    TimeSpan? TargetTime);

/// <summary>A validáció után már biztosan teljes, érvényes bemenet.</summary>
public record ValidGoalInput(GoalType Type, decimal DistanceKm, DateOnly TargetDate, int? TargetTimeSeconds);

/// <summary>Egy cél a kliens felé.</summary>
public record GoalResponse(
    Guid Id,
    Guid RunnerId,
    GoalType Type,
    decimal DistanceKm,
    DateOnly TargetDate,
    TimeSpan? TargetTime,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt)
{
    /// <summary>
    /// Válasz-DTO az entitásból; a célidőt TimeSpan-ként (óó:pp:mm) adja vissza.
    /// </summary>
    public static GoalResponse From(Goal g) => new(
        g.Id, g.RunnerId, g.Type, g.DistanceKm, g.TargetDate,
        g.TargetTimeSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
        g.IsActive, g.CreatedAt, g.ArchivedAt);
}

/// <summary>
/// A célnézet (spec 3.3): az aktív cél, a céldátumig hátralévő hetek és a legutóbbi edzések.
/// </summary>
public record ActiveGoalResponse(GoalResponse Goal, int WeeksRemaining, List<WorkoutResponse> RecentWorkouts);
