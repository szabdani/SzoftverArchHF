using Training.Api.Workouts;

namespace Training.Api.Goals;

/// <summary>
/// Cél kitűzése vagy módosítása. A táv csak egyéni (custom) célnál kell.
/// A célidő formátuma óó:pp:mm (pl. "01:45:00").
/// </summary>
public record GoalRequest(GoalType? Type, decimal? DistanceKm, DateOnly? TargetDate, TimeSpan? TargetTime);

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
