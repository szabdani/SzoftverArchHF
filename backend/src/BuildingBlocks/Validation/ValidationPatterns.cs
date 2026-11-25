namespace BuildingBlocks.Validation;

/// <summary>
/// Reguláris kifejezések az óó:pp:mm formátumú időkhöz. A határokat is a minta fejezi ki, így az
/// OpenAPI sémában (és a belőle generált frontend sémában) is pontosan ugyanaz a szabály szerepel.
/// </summary>
public static class ValidationPatterns
{
    /// <summary>1 perc és 10 óra között, egész másodpercre: 00:01:00 … 10:00:00.</summary>
    public const string Duration1MinTo10Hours =
        @"^(?:00:(?:0[1-9]|[1-5][0-9]):[0-5][0-9]|0[1-9]:[0-5][0-9]:[0-5][0-9]|10:00:00)$";
}
