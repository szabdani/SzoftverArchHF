using Contracts;
using Contracts.Training;

namespace Training.Api.Workouts;

/// <summary>Egy elvégzett, rögzített futóedzés (spec 3.2).</summary>
public class Workout
{
    public const int NoteMaxLength = 500;

    private Workout() { }

    public Guid Id { get; private set; }
    public Guid RunnerId { get; private set; }
    public DateOnly Date { get; private set; }
    public WorkoutType Type { get; private set; }
    public decimal DistanceKm { get; private set; }
    public int DurationSeconds { get; private set; }
    public int? AvgHeartRate { get; private set; }
    public int? ElevationGainM { get; private set; }
    public int Rpe { get; private set; }
    public string? Note { get; private set; }

    /// <summary>Edzésterhelés (sRPE): RPE × időtartam percben. Mentéskor számoljuk.</summary>
    public double Load { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Tempó másodperc/km-ben (időtartam / táv).</summary>
    public int PaceSecondsPerKm => CalculatePaceSecondsPerKm(DurationSeconds, DistanceKm);

    public static Workout Create(Guid runnerId, ValidWorkoutInput input, DateTimeOffset now)
    {
        var workout = new Workout { Id = Guid.CreateVersion7(now), RunnerId = runnerId, CreatedAt = now };
        workout.Apply(input, now);
        return workout;
    }

    public void Update(ValidWorkoutInput input, DateTimeOffset now) => Apply(input, now);

    public WorkoutSnapshot ToSnapshot() =>
        new(Id, RunnerId, Date, Type, DistanceKm, DurationSeconds, Rpe, AvgHeartRate, ElevationGainM, Load);

    public static double CalculateLoad(int rpe, int durationSeconds) =>
        Math.Round(rpe * durationSeconds / 60.0, 1, MidpointRounding.AwayFromZero);

    public static int CalculatePaceSecondsPerKm(int durationSeconds, decimal distanceKm) =>
        (int)Math.Round(durationSeconds / distanceKm, MidpointRounding.AwayFromZero);

    private void Apply(ValidWorkoutInput input, DateTimeOffset now)
    {
        Date = input.Date;
        Type = input.Type;
        DistanceKm = input.DistanceKm;
        DurationSeconds = input.DurationSeconds;
        AvgHeartRate = input.AvgHeartRate;
        ElevationGainM = input.ElevationGainM;
        Rpe = input.Rpe;
        Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        Load = CalculateLoad(input.Rpe, input.DurationSeconds);
        UpdatedAt = now;
    }
}
