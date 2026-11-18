namespace Training.Api.Goals;

/// <summary>
/// Céltípusok (spec 3.3). A cél típusa később az edzésjavaslatot is befolyásolja (spec 3.7).
/// </summary>
public enum GoalType
{
    FiveK,
    TenK,
    HalfMarathon,
    Marathon,
    Custom,
}

/// <summary>
/// A futó által kitűzött verseny- vagy távcél céldátummal és opcionális célidővel (UC05).
/// Egy futónak egyszerre egy aktív célja lehet, a korábbiak archiválódnak.
/// </summary>
public class Goal
{
    private Goal() { }

    public Guid Id { get; private set; }
    public Guid RunnerId { get; private set; }
    public GoalType Type { get; private set; }

    /// <summary>A cél távja km-ben; a fix típusoknál a típusból adódik.</summary>
    public decimal DistanceKm { get; private set; }

    public DateOnly TargetDate { get; private set; }
    public int? TargetTimeSeconds { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>
    /// Új, aktív cél a futó nevére.
    /// </summary>
    public static Goal Create(Guid runnerId, ValidGoalInput input, DateTimeOffset now)
    {
        var goal = new Goal
        {
            Id = Guid.CreateVersion7(now),
            RunnerId = runnerId,
            IsActive = true,
            CreatedAt = now,
        };
        goal.Update(input);
        return goal;
    }

    /// <summary>
    /// A cél adatainak felülírása.
    /// </summary>
    public void Update(ValidGoalInput input)
    {
        Type = input.Type;
        DistanceKm = input.DistanceKm;
        TargetDate = input.TargetDate;
        TargetTimeSeconds = input.TargetTimeSeconds;
    }

    /// <summary>
    /// A cél archiválása (új aktív cél kitűzésekor).
    /// </summary>
    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        ArchivedAt = now;
    }

    /// <summary>
    /// A cél távja: a fix típusoknál a szabványos táv, egyéni célnál a megadott.
    /// </summary>
    public static decimal DistanceFor(GoalType type, decimal customDistanceKm) => type switch
    {
        GoalType.FiveK => 5m,
        GoalType.TenK => 10m,
        GoalType.HalfMarathon => 21.0975m,
        GoalType.Marathon => 42.195m,
        _ => customDistanceKm,
    };

    /// <summary>
    /// A céldátumig hátralévő hetek száma, felfelé kerekítve (10 nap = 2 hét); múltbeli dátumnál 0.
    /// </summary>
    public static int WeeksUntil(DateOnly targetDate, DateOnly today)
    {
        var days = targetDate.DayNumber - today.DayNumber;
        return days <= 0 ? 0 : (days + 6) / 7;
    }
}
