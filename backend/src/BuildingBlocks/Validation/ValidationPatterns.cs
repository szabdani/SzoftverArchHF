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

    /// <summary>1 perc és 23:59:59 között, egész másodpercre (egy napnál hosszabb idő nem óó:pp:mm).</summary>
    public const string Duration1MinTo24Hours =
        @"^(?:00:(?:0[1-9]|[1-5][0-9]):[0-5][0-9]|(?:0[1-9]|1[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9])$";
}
