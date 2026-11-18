using Contracts;

namespace Analytics.Api;

/// <summary>
/// Az edzéstípusok magyar neve a felhasználónak szóló szövegekhez (indoklások, figyelmeztetések).
/// </summary>
public static class WorkoutTypeNames
{
    /// <summary>
    /// Az edzéstípus magyar neve, kisbetűvel (pl. tempó).
    /// </summary>
    public static string Hungarian(WorkoutType type) => type switch
    {
        WorkoutType.Easy => "könnyű",
        WorkoutType.Recovery => "regeneráló",
        WorkoutType.Tempo => "tempó",
        WorkoutType.Interval => "intervall",
        WorkoutType.Long => "hosszú",
        WorkoutType.Race => "verseny",
        _ => type.ToString(),
    };
}
