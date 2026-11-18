using Contracts;
using Contracts.Training;

namespace Training.Api.Plans;

/// <summary>
/// Egy edzésterv egy napra szóló eleme. Állapota tervezett, teljesített (UC08) vagy kihagyott;
/// a kihagyott állapotot az ütemezett ellenőrzés állítja be (<see cref="MissedWorkoutJob"/>).
/// </summary>
public class PlannedWorkout
{
    public const int NoteMaxLength = 500;

    private PlannedWorkout() { }

    public Guid Id { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid RunnerId { get; private set; }
    public DateOnly Date { get; private set; }
    public WorkoutType Type { get; private set; }
    public decimal? PlannedDistanceKm { get; private set; }
    public int? PlannedDurationSeconds { get; private set; }
    public int TargetRpe { get; private set; }
    public string? Note { get; private set; }
    public PlannedWorkoutStatus Status { get; private set; }

    /// <summary>A teljesítő edzés azonosítója, ha már teljesült.</summary>
    public Guid? WorkoutId { get; private set; }

    /// <summary>
    /// Új, tervezett állapotú edzés a terv futójának.
    /// </summary>
    public static PlannedWorkout Create(TrainingPlan plan, ValidPlannedWorkoutInput input)
    {
        var plannedWorkout = new PlannedWorkout
        {
            Id = Guid.CreateVersion7(),
            PlanId = plan.Id,
            RunnerId = plan.RunnerId,
            Status = PlannedWorkoutStatus.Planned,
        };
        plannedWorkout.Apply(input);
        return plannedWorkout;
    }

    /// <summary>
    /// Módosítás vagy áthelyezés másik napra (drag &amp; drop). Egy kihagyott edzés mai vagy
    /// későbbi napra téve újra tervezett lesz.
    /// </summary>
    public void Update(ValidPlannedWorkoutInput input, DateOnly today)
    {
        Apply(input);
        if (Status == PlannedWorkoutStatus.Missed && Date >= today)
        {
            Status = PlannedWorkoutStatus.Planned;
        }
    }

    /// <summary>
    /// A futó rögzített hozzá egy edzést: teljesítettre vált (UC08).
    /// </summary>
    public void Complete(Guid workoutId)
    {
        Status = PlannedWorkoutStatus.Completed;
        WorkoutId = workoutId;
    }

    /// <summary>
    /// A teljesítő edzést törölték: újra tervezett lesz (ha a napja elmúlt, az ellenőrzés kihagyottra állítja).
    /// </summary>
    public void Reopen()
    {
        Status = PlannedWorkoutStatus.Planned;
        WorkoutId = null;
    }

    /// <summary>
    /// A napja végéig nem teljesült: kihagyott.
    /// </summary>
    public void MarkMissed() => Status = PlannedWorkoutStatus.Missed;

    /// <summary>
    /// A tervezett edzés állapota az integrációs eseményhez (PlannedWorkoutChanged).
    /// </summary>
    public PlannedWorkoutSnapshot ToSnapshot() =>
        new(Id, RunnerId, Date, Type, PlannedDistanceKm, PlannedDurationSeconds, TargetRpe, Status);

    /// <summary>
    /// A bemenet beírása a mezőkbe; az üres megjegyzésből null lesz.
    /// </summary>
    private void Apply(ValidPlannedWorkoutInput input)
    {
        Date = input.Date;
        Type = input.Type;
        PlannedDistanceKm = input.PlannedDistanceKm;
        PlannedDurationSeconds = input.PlannedDurationSeconds;
        TargetRpe = input.TargetRpe;
        Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
    }
}
