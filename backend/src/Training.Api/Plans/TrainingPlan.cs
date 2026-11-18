namespace Training.Api.Plans;

/// <summary>
/// Edzésterv (UC11): egy időszakra szóló terv tervezett edzésekből, opcionálisan célhoz kötve.
/// A futó saját magának, az edző a hozzárendelt futónak készítheti.
/// </summary>
public class TrainingPlan
{
    public const int NameMaxLength = 100;

    private TrainingPlan() { }

    public Guid Id { get; private set; }
    public Guid RunnerId { get; private set; }

    /// <summary>A tervet létrehozó felhasználó: maga a futó, vagy az edzője.</summary>
    public Guid CreatedById { get; private set; }

    public string Name { get; private set; } = "";
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public Guid? GoalId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<PlannedWorkout> Workouts { get; private set; } = [];

    /// <summary>
    /// Új terv a futó nevére.
    /// </summary>
    public static TrainingPlan Create(Guid runnerId, Guid createdById, ValidPlanInput input, Guid? goalId, DateTimeOffset now)
    {
        var plan = new TrainingPlan
        {
            Id = Guid.CreateVersion7(now),
            RunnerId = runnerId,
            CreatedById = createdById,
            CreatedAt = now,
        };
        plan.Update(input, goalId);
        return plan;
    }

    /// <summary>
    /// A terv adatainak felülírása.
    /// </summary>
    public void Update(ValidPlanInput input, Guid? goalId)
    {
        Name = input.Name;
        StartDate = input.StartDate;
        EndDate = input.EndDate;
        GoalId = goalId;
    }
}
