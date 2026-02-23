namespace Application.Messaging;

/// <summary>
/// RabbitMQ queue adlari (topology sabitleri).
/// </summary>
public static class QueueNames
{
    public const string AdPasswordChangeCommands = "ad.passwordchange.commands";
    public const string ServerDiscoveryCommands = "server.discovery.commands";
    public const string ServerUpdateCommands = "server.update.commands";
    public const string ServerVerifyCommands = "server.verify.commands";
    public const string ServerResultEvents = "server.result.events";
}
