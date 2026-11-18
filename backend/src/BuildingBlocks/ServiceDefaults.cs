using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks;

public static class ServiceDefaults
{
    /// <summary>Minden szolgáltatásban azonos alapbeállítások: hibakezelés, JSON, hitelesítés, health check, OpenAPI.</summary>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        builder.Services.AddKeycloakAuthentication(builder.Configuration);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>Az alap middleware-ek (hibakezelés, hitelesítés, jogosultság) és a /health, /openapi végpontok.</summary>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapHealthChecks("/health");
        return app;
    }
}
