using Contracts;
using Contracts.Demo;
using Training.Api.Goals;
using Training.Api.Plans;
using Training.Api.Workouts;

namespace Training.Api.Demo;

/// <summary>
/// Egy tervezett demóedzés: ha elvégezték, a hozzá tartozó edzés is megvan; ha nem, kihagyott
/// (múltbeli) vagy tervezett (mai és jövőbeli).
/// </summary>
public record DemoPlannedWorkout(ValidPlannedWorkoutInput Planned, ValidWorkoutInput? Workout, bool Missed);

/// <summary>Egy szimulált futó teljes adatkészlete: cél, terv, tervezett és elvégzett edzések.</summary>
public record DemoData(ValidGoalInput Goal, ValidPlanInput Plan, List<DemoPlannedWorkout> PlannedWorkouts);

/// <summary>
/// Szimulált futók 12 hetes adata a négy jellegzetes mintával (spec 3.2): egyenletes építkezés,
/// hirtelen terhelésugrás, gyakori kihagyás, túl sok intenzív nap. Mintánként rögzített véletlenmaggal
/// dolgozik, így ugyanarra a napra mindig ugyanazt adja.
/// </summary>
public static class DemoDataGenerator
{
    public const int Weeks = 12;
    public const int FutureDays = 14;

    /// <summary>Egy hét edzésnapjai (hétfő = 0): típus és a heti táv ekkora része. Hétfő és péntek pihenő.</summary>
    private static readonly (int Day, WorkoutType Type, decimal Share)[] WeekTemplate =
    [
        (1, WorkoutType.Easy, 0.18m),
        (2, WorkoutType.Interval, 0.18m),
        (3, WorkoutType.Easy, 0.16m),
        (5, WorkoutType.Long, 0.32m),
        (6, WorkoutType.Recovery, 0.16m),
    ];

    /// <summary>
    /// A minta adatai a mai napig (a mai és a következő két hét edzései még csak tervezettek).
    /// </summary>
    public static DemoData Generate(DemoRunnerPattern pattern, DateOnly today)
    {
        var random = new Random(1000 + (int)pattern);
        var firstMonday = WeekStart(today).AddDays(-7 * (Weeks - 1));
        var lastDay = today.AddDays(FutureDays - 1);
        var plannedWorkouts = new List<DemoPlannedWorkout>();
        var missedRecently = 0;

        for (var date = firstMonday; date <= lastDay; date = date.AddDays(1))
        {
            // Pihenőnapon (hétfő, péntek) nincs edzés.
            var dayOfWeek = ((int)date.DayOfWeek + 6) % 7;
            if (!WeekTemplate.Any(t => t.Day == dayOfWeek))
            {
                continue;
            }

            var template = WeekTemplate.Single(t => t.Day == dayOfWeek);
            var week = (date.DayNumber - firstMonday.DayNumber) / 7;
            var type = QualityType(pattern, template.Type);
            var distance = Math.Max(3m, decimal.Round(WeeklyKm(pattern, week, date, today) * template.Share * Jitter(random), 1));
            var rpe = Rpe(pattern, type, random);
            var planned = new ValidPlannedWorkoutInput(date, type, distance, null, rpe, null);

            // A mai és a jövőbeli napok még csak tervezettek.
            if (date >= today)
            {
                plannedWorkouts.Add(new DemoPlannedWorkout(planned, null, false));
                continue;
            }

            if (IsMissed(pattern, date, today, random, ref missedRecently))
            {
                plannedWorkouts.Add(new DemoPlannedWorkout(planned, null, true));
                continue;
            }

            var paceSeconds = (int)(PaceSecondsPerKm(type) * Jitter(random));
            var workout = new ValidWorkoutInput(
                date,
                type,
                distance,
                (int)(distance * paceSeconds),
                HeartRate(type) + random.Next(-5, 6),
                random.Next(0, 150),
                rpe,
                null);
            plannedWorkouts.Add(new DemoPlannedWorkout(planned, workout, false));
        }

        return new DemoData(Goal(pattern, today), new ValidPlanInput($"Demó: {PatternName(pattern)}", firstMonday, lastDay, true), plannedWorkouts);
    }

    /// <summary>
    /// A nap hetének hétfője.
    /// </summary>
    public static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>
    /// A heti táv a minta szerint: egyenletes építkezésnél hetente +5%, ugrásnál az utolsó 7 nap
    /// a megszokott háromszorosa (ACWR kb. 1,8), a többinél nagyjából állandó.
    /// </summary>
    private static decimal WeeklyKm(DemoRunnerPattern pattern, int week, DateOnly date, DateOnly today) => pattern switch
    {
        DemoRunnerPattern.SteadyBuild => 25m * (decimal)Math.Pow(1.05, week),
        DemoRunnerPattern.SuddenSpike => date >= today.AddDays(-6) ? 30m * 3m : 30m,
        DemoRunnerPattern.FrequentSkipping => 25m,
        _ => 30m,
    };

    /// <summary>
    /// A nap edzéstípusa a mintához igazítva: a félmaratonra és maratonra készülőknek a minőségi edzés
    /// tempó (a többieknek intervall); a túl intenzív mintánál a regeneráló futás helyett is tempó megy.
    /// </summary>
    private static WorkoutType QualityType(DemoRunnerPattern pattern, WorkoutType type) => (pattern, type) switch
    {
        (DemoRunnerPattern.SteadyBuild or DemoRunnerPattern.SuddenSpike, WorkoutType.Interval) => WorkoutType.Tempo,
        (DemoRunnerPattern.TooManyIntenseDays, WorkoutType.Recovery) => WorkoutType.Tempo,
        _ => type,
    };

    /// <summary>
    /// Az érzett terhelés: a túl intenzív mintánál minden edzés intenzív (kedd–csütörtök és
    /// szombat–vasárnap egymást követő intenzív napok); a többinél a szokásos értékek.
    /// </summary>
    private static int Rpe(DemoRunnerPattern pattern, WorkoutType type, Random random)
    {
        if (pattern == DemoRunnerPattern.TooManyIntenseDays)
        {
            return random.Next(7, 9);
        }

        return type switch
        {
            WorkoutType.Interval => 8,
            WorkoutType.Tempo => 7,
            WorkoutType.Long => 5,
            WorkoutType.Recovery => 2,
            _ => random.Next(3, 5),
        };
    }

    /// <summary>
    /// Kihagyott-e a múltbeli tervezett edzés: a gyakran kihagyó mintánál kb. 35%, és az utolsó 7 nap
    /// első két edzése biztosan; a többi mintánál soha.
    /// </summary>
    private static bool IsMissed(DemoRunnerPattern pattern, DateOnly date, DateOnly today, Random random, ref int missedRecently)
    {
        if (pattern != DemoRunnerPattern.FrequentSkipping)
        {
            return false;
        }

        if (date >= today.AddDays(-6) && missedRecently < 2)
        {
            missedRecently++;
            return true;
        }

        return random.NextDouble() < 0.35;
    }

    /// <summary>
    /// Tipikus tempó típusonként (mp/km).
    /// </summary>
    private static int PaceSecondsPerKm(WorkoutType type) => type switch
    {
        WorkoutType.Interval => 285,
        WorkoutType.Tempo => 300,
        WorkoutType.Long => 375,
        WorkoutType.Recovery => 390,
        _ => 360,
    };

    /// <summary>
    /// Tipikus átlagpulzus típusonként.
    /// </summary>
    private static int HeartRate(WorkoutType type) => type switch
    {
        WorkoutType.Interval => 170,
        WorkoutType.Tempo => 165,
        WorkoutType.Long => 148,
        WorkoutType.Recovery => 130,
        _ => 140,
    };

    /// <summary>
    /// ±10% véletlen eltérés, hogy az adat ne legyen gépiesen egyforma.
    /// </summary>
    private static decimal Jitter(Random random) => 0.9m + (decimal)random.NextDouble() * 0.2m;

    /// <summary>
    /// A minta célja: félmaraton, maraton, 10 km vagy 5 km.
    /// </summary>
    private static ValidGoalInput Goal(DemoRunnerPattern pattern, DateOnly today) => pattern switch
    {
        DemoRunnerPattern.SteadyBuild => new(GoalType.HalfMarathon, 21.0975m, today.AddDays(70), 6600),
        DemoRunnerPattern.SuddenSpike => new(GoalType.Marathon, 42.195m, today.AddDays(120), null),
        DemoRunnerPattern.FrequentSkipping => new(GoalType.TenK, 10m, today.AddDays(42), 3300),
        _ => new(GoalType.FiveK, 5m, today.AddDays(28), 1380),
    };

    /// <summary>
    /// A minta magyar neve a terv nevéhez.
    /// </summary>
    private static string PatternName(DemoRunnerPattern pattern) => pattern switch
    {
        DemoRunnerPattern.SteadyBuild => "egyenletes építkezés",
        DemoRunnerPattern.SuddenSpike => "hirtelen terhelésugrás",
        DemoRunnerPattern.FrequentSkipping => "gyakori kihagyás",
        _ => "túl sok intenzív nap",
    };
}
