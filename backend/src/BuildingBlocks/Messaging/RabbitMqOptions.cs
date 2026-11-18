namespace BuildingBlocks.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "futoedzes";
    public string Password { get; init; } = "futoedzes";
}
