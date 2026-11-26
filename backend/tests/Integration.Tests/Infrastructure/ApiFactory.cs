using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Egy API szolgáltatás (Training vagy Analytics) WebApplicationFactory-ja az integrációs tesztekhez.
/// A valós rendszer indul el (migrációk, MassTransit, outbox), de a külső függőségek a Testcontainers
/// konténerekre mutatnak, a hitelesítés pedig a fejléc-alapú teszt-sémára cserélődik (Keycloak nélkül).
///
/// A típusparaméter egy tetszőleges, az adott API assembly-jéből származó típus (pl. egy service), hogy a
/// WebApplicationFactory egyértelműen azt az assembly-t azonosítsa belépési pontként – a Program osztály
/// top-level és mindkét szolgáltatásban azonos nevű, ezért nem használható közvetlenül.
/// </summary>
/// <typeparam name="TEntryMarker">Az API assembly egy típusa (belépési pont azonosításához).</typeparam>
public class ApiFactory<TEntryMarker>(ContainerFixture containers, string databaseName) : WebApplicationFactory<TEntryMarker>
    where TEntryMarker : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting a host-konfigurációba ír, ami felülírja az appsettings.json értékeit (az
        // appsettings alap 5442/5443 portjait a Testcontainers dinamikus portjaira cserélve).
        builder.UseSetting("ConnectionStrings:Default", containers.ConnectionStringFor(databaseName));
        builder.UseSetting("RabbitMq:Host", containers.RabbitHost);
        builder.UseSetting("RabbitMq:Port", containers.RabbitPort.ToString());
        builder.UseSetting("RabbitMq:Username", containers.RabbitUsername);
        builder.UseSetting("RabbitMq:Password", containers.RabbitPassword);
        builder.UseSetting("RabbitMq:VirtualHost", "/");
        // A Keycloak Authority kötelező mező; tesztben nem használjuk (teszt-auth séma), de a kötés ne bukjon.
        builder.UseSetting("Keycloak:Authority", "http://localhost/auth/realms/futoedzes");
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");

        builder.ConfigureTestServices(services =>
        {
            // A valódi JWT helyett a fejléc-alapú teszt-séma lesz az alapértelmezett; a policy-k (role claim) maradnak.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

/// <summary>A Training.Api factory-ja (egy Training assembly-beli típussal mint belépési pont).</summary>
public sealed class TrainingApiFactory(ContainerFixture containers)
    : ApiFactory<Training.Api.Workouts.WorkoutService>(containers, "training");

/// <summary>Az Analytics.Api factory-ja (egy Analytics assembly-beli típussal mint belépési pont).</summary>
public sealed class AnalyticsApiFactory(ContainerFixture containers)
    : ApiFactory<Analytics.Api.Risk.RiskService>(containers, "analytics");
