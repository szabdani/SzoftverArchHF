using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BuildingBlocks.Validation;

/// <summary>
/// A kérések mezőnkénti szabályai a kérés-DTO-k attribútumaiban vannak (Required, Range, MaxLength,
/// RegularExpression, EmailAddress, EnumDataType). A szerver ezekkel validál, és ugyanezek kerülnek az
/// OpenAPI leírásba, amiből a frontend a saját sémáját generálja: így a kliens és a szerver ugyanazzal
/// a sémával validál (spec 3.2).
/// </summary>
public static class RequestValidation
{
    /// <summary>
    /// Ellenőrzi a kérés attribútumait. A hibakulcsok a JSON mezőnevek (camelCase), az üzenetek az
    /// attribútumok magyar hibaüzenetei.
    /// </summary>
    public static Dictionary<string, string[]> ValidateAnnotations(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results
            .SelectMany(result => result.MemberNames.Select(member => (
                Field: JsonNamingPolicy.CamelCase.ConvertName(member),
                Message: result.ErrorMessage ?? "Érvénytelen érték.")))
            .GroupBy(error => error.Field)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());
    }
}
