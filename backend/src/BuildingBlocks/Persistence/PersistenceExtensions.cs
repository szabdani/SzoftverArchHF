using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Persistence;

public static class PersistenceExtensions
{
    /// <summary>
    /// PostgreSQL DbContext a <c>ConnectionStrings:Default</c> alapján, snake_case táblanevekkel,
    /// és adatbázis health check.
    /// </summary>
    public static IServiceCollection AddPostgresDbContext<TDbContext>(this IServiceCollection services, IConfiguration configuration)
        where TDbContext : DbContext
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Hiányzik a 'ConnectionStrings:Default' konfiguráció.");

        services.AddDbContext<TDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks().AddDbContextCheck<TDbContext>("database");
        return services;
    }

    /// <summary>A MassTransit outbox/inbox táblái; minden szolgáltatás DbContextjében kell.</summary>
    public static ModelBuilder AddMessagingOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        return modelBuilder;
    }

    /// <summary>Induláskor alkalmazza a függő migrációkat (fejlesztői és demó környezethez).</summary>
    public static async Task MigrateDatabaseAsync<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.Database.MigrateAsync();
    }
}
