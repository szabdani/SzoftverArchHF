using MassTransit;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// Egy önálló MassTransit busz, amely csak publikál (consumer és outbox nélkül) a közös RabbitMQ-ra. Így a
/// tesztek kiválthatják azokat az eseményeket, amelyeket éles helyzetben az Identity szolgáltatás küldene
/// (pl. `CoachRunnerLinked`, `DemoDataRequested`), anélkül hogy az Identity-t is fel kellene húzni.
/// </summary>
public sealed class TestPublisher : IAsyncDisposable
{
    private readonly IBusControl bus;

    private TestPublisher(IBusControl bus) => this.bus = bus;

    /// <summary>Elindít egy publikáló buszt a megadott RabbitMQ konténerhez.</summary>
    public static async Task<TestPublisher> StartAsync(ContainerFixture containers)
    {
        var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(new Uri($"rabbitmq://{containers.RabbitHost}:{containers.RabbitPort}/"), host =>
            {
                host.Username(containers.RabbitUsername);
                host.Password(containers.RabbitPassword);
            });
        });

        await bus.StartAsync();
        return new TestPublisher(bus);
    }

    /// <summary>Közzétesz egy integrációs eseményt a közös brókeren.</summary>
    public Task PublishAsync<T>(T message) where T : class => bus.Publish(message);

    public async ValueTask DisposeAsync() => await bus.StopAsync();
}
