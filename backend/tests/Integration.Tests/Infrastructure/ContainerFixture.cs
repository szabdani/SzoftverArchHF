using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Közös infrastruktúra az integrációs tesztekhez: egy PostgreSQL és egy RabbitMQ konténer, amelyeket a
/// teszt-gyűjtemény egyszer indít el és állít le. A Training és az Analytics külön adatbázist kap ugyanazon
/// a Postgres szerveren, de közös RabbitMQ-t, hogy az esemény (Training → Analytics) valóban átmenjen.
/// A valódi broker + adatbázis azért kell, mert a rendszer EF tranzakciós outboxot használ, amit az
/// in-memory teszt-harness nem modellez hűen.
/// </summary>
public sealed class ContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18.7-alpine")
        .Build();

    // Saját felhasználó: a beépített `guest` csak localhostról engedélyezett, a mapped porton át viszont
    // a RabbitMQ nem localhostként látja a kapcsolatot, és elutasítaná.
    private const string RabbitUser = "futoedzes";
    private const string RabbitPass = "futoedzes";

    private readonly RabbitMqContainer rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.4.0-management")
        .WithUsername(RabbitUser)
        .WithPassword(RabbitPass)
        .Build();

    /// <summary>A RabbitMQ host (a MassTransit `RabbitMq:Host` konfigja).</summary>
    public string RabbitHost { get; private set; } = "";

    /// <summary>A RabbitMQ felhasználónév és jelszó.</summary>
    public string RabbitUsername => RabbitUser;
    public string RabbitPassword => RabbitPass;

    /// <summary>A Testcontainers által kiosztott RabbitMQ AMQP port (dinamikus).</summary>
    public int RabbitPort { get; private set; }

    /// <summary>Egy adott szolgáltatás adatbázisának kapcsolati sztringje ugyanazon a Postgres szerveren.</summary>
    public string ConnectionStringFor(string database)
    {
        // A Testcontainers alap kapcsolati sztringjéből (Database=postgres) cseréljük az adatbázist.
        var baseConn = postgres.GetConnectionString();
        return baseConn.Replace("Database=postgres", $"Database={database}");
    }

    /// <summary>Elindítja a konténereket, és létrehozza a két szolgáltatás adatbázisát.</summary>
    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), rabbit.StartAsync());

        // A migrációk induláskor a meglévő adatbázisra futnak, ezért előre létrehozzuk őket.
        await postgres.ExecScriptAsync("CREATE DATABASE training;");
        await postgres.ExecScriptAsync("CREATE DATABASE analytics;");

        RabbitHost = rabbit.Hostname;
        RabbitPort = rabbit.GetMappedPublicPort(5672);
    }

    /// <summary>Leállítja és eldobja a konténereket.</summary>
    public async ValueTask DisposeAsync()
    {
        await rabbit.DisposeAsync();
        await postgres.DisposeAsync();
    }
}

/// <summary>A konténer-fixture egy xUnit gyűjteményen osztozik, hogy ne induljon újra tesztenként.</summary>
[CollectionDefinition(Name)]
public sealed class ContainerCollection : ICollectionFixture<ContainerFixture>
{
    public const string Name = "containers";
}
