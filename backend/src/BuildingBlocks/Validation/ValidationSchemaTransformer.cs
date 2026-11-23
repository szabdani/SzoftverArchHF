using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BuildingBlocks.Validation;

/// <summary>
/// Az OpenAPI sémát a kérés-DTO-k validációs attribútumaihoz igazítja, hogy a frontendnek generált séma
/// azt fogadja el, amit a szerver: a [Required] és a nem-nullable (érték- vagy referencia-) típusú mező
/// kötelező és nem lehet null, a többi opcionális; az [EmailAddress] mező e-mail formátumú, az [AllowedValues] mező felsorolás;
/// az enumok értékei között nincs null.
/// </summary>
public static class ValidationSchemaTransformer
{
    /// <summary>
    /// Egy séma átalakítása (az OpenAPI generátor minden sémára meghívja).
    /// </summary>
    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken ct)
    {
        // A nullable enum mezők miatt a generátor null-t is felvesz az enum értékei közé; a null-t a
        // mezőszintű séma kezeli (oneOf), az enum értékei közé nem való.
        if (schema.Enum is { Count: > 0 } values)
        {
            for (var i = values.Count - 1; i >= 0; i--)
            {
                if (values[i] is null)
                {
                    values.RemoveAt(i);
                }
            }
        }

        if (context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object || schema.Properties is null)
        {
            return Task.CompletedTask;
        }

        foreach (var property in context.JsonTypeInfo.Properties)
        {
            if (!schema.Properties.TryGetValue(property.Name, out var propertySchema))
            {
                continue;
            }

            var attributes = property.AttributeProvider;

            // Kötelező a [Required] és a nem-nullable típusú mező (a kérésekben ezt a szerver is mindig várja,
            // a válaszokban mindig benne van); a generátor egyébként minden konstruktor-paramétert kötelezőnek venne.
            if (IsNonNullable(property) || attributes?.IsDefined(typeof(RequiredAttribute), inherit: true) == true)
            {
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(property.Name);
                schema.Properties[property.Name] = WithoutNull(propertySchema);
            }
            else
            {
                schema.Required?.Remove(property.Name);
            }

            if (attributes?.IsDefined(typeof(EmailAddressAttribute), inherit: true) == true && propertySchema is OpenApiSchema emailSchema)
            {
                emailSchema.Format = "email";
            }

            if (attributes?.GetCustomAttributes(typeof(AllowedValuesAttribute), inherit: true).FirstOrDefault() is AllowedValuesAttribute allowed
                && propertySchema is OpenApiSchema allowedSchema)
            {
                allowedSchema.Enum = allowed.Values.Select(value => (JsonNode)JsonValue.Create(value?.ToString())!).ToList();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A kész dokumentumban a kötelező mezők „null vagy hivatkozás” (oneOf) sémája helyére maga a hivatkozás
    /// kerül. Ezt a burkolást (pl. nullable enumnál) a generátor a sématranszformátorok után teszi rá.
    /// </summary>
    public static Task TransformDocumentAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        foreach (var schema in document.Components?.Schemas?.Values.OfType<OpenApiSchema>() ?? [])
        {
            if (schema.Properties is null || schema.Required is null)
            {
                continue;
            }

            foreach (var name in schema.Required)
            {
                if (schema.Properties.TryGetValue(name, out var propertySchema) && propertySchema is OpenApiSchema { OneOf.Count: > 0 } wrapper)
                {
                    var nonNullOptions = wrapper.OneOf.Where(option => option is not OpenApiSchema { Type: JsonSchemaType.Null }).ToList();
                    if (nonNullOptions.Count == 1)
                    {
                        schema.Properties[name] = nonNullOptions[0];
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Nem-nullable-e a mező: értéktípusnál a Nullable&lt;T&gt; hiánya, referencia-típusnál a C# nullable
    /// annotáció (pl. string vagy string?) dönt.
    /// </summary>
    private static bool IsNonNullable(JsonPropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) is null;
        }

        return property.AttributeProvider is PropertyInfo info
            && new NullabilityInfoContext().Create(info).ReadState == NullabilityState.NotNull;
    }

    /// <summary>
    /// A mező sémája null nélkül (a típusból kikerül a null).
    /// </summary>
    private static IOpenApiSchema WithoutNull(IOpenApiSchema propertySchema)
    {
        if (propertySchema is OpenApiSchema { Type: { } type } schema)
        {
            schema.Type = type & ~JsonSchemaType.Null;
        }

        return propertySchema;
    }
}
