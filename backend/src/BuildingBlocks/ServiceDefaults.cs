using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Auth;
using BuildingBlocks.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks;

public static class ServiceDefaults
{
    /// <summary>Minden szolgáltatásban azonos alapbeállítások: hibakezelés, JSON, hitelesítés, health check, OpenAPI.</summary>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer(ValidationSchemaTransformer.TransformAsync);
            options.AddDocumentTransformer(ValidationSchemaTransformer.TransformDocumentAsync);
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            // A szám mezőkbe csak szám kerülhet (a webes alapbeállítás szövegből is olvasna), így a
            // séma is tisztán number/integer típusú.
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });
        builder.Services.AddKeycloakAuthentication(builder.Configuration);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>
    /// Az alap middleware-ek (hibakezelés, hitelesítés, jogosultság), a /health végpont, és fejlesztői
    /// környezetben az OpenAPI leírás a szolgáltatás útvonalán (pl. /api/training/openapi/v1.json),
    /// hogy a gatewayen át is elérhető legyen.
    /// </summary>
    /// <param name="serviceName">A szolgáltatás útvonal-előtagja: training, identity vagy analytics.</param>
    public static WebApplication UseServiceDefaults(this WebApplication app, string serviceName)
    {
        // A hibás kérés (pl. nem értelmezhető JSON) 400 maradjon, ne legyen belőle 500.
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
                ? badRequest.StatusCode
                : StatusCodes.Status500InternalServerError,
        });
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi($"/api/{serviceName}/openapi/{{documentName}}.json");
        }

        app.MapHealthChecks("/health");
        return app;
    }
}
