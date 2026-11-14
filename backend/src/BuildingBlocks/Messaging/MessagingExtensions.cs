using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// MassTransit + RabbitMQ, tranzakciós outbox-szal: a kiküldött események ugyanabban a
    /// tranzakcióban kerülnek az adatbázisba, mint az üzleti változás, így nem veszhetnek el.
    /// A fogadó oldalon az inbox kiszűri a duplán kézbesített üzeneteket.
    /// </summary>
    /// <param name="serviceName">Queue-név előtag, hogy két szolgáltatás azonos nevű consumere ne osztozzon egy queue-n.</param>
    public static IServiceCollection AddMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
        where TDbContext : DbContext
    {
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.AddMassTransit(bus =>
        {
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, includeNamespace: false));

            bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(r => r.Intervals(100, 500, 1000, 5000));
                endpoint.UseEntityFrameworkOutbox<TDbContext>(context);
            });

            configureConsumers?.Invoke(bus);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, rabbit.VirtualHost, host =>
                {
                    host.Username(rabbit.Username);
                    host.Password(rabbit.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
