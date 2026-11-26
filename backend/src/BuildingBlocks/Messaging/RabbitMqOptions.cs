namespace BuildingBlocks.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";

    /// <summary>Az AMQP port; a compose-ban az alapértelmezett 5672, teszteléskor a konténer dinamikus portja.</summary>
    public ushort Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "futoedzes";
    public string Password { get; init; } = "futoedzes";
}
