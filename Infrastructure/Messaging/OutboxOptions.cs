namespace Infrastructure.Messaging;

/// <summary>
/// MassTransit EF outbox runtime ayarlari.
/// </summary>
public sealed class OutboxOptions
{
    public bool Enabled { get; set; } = true;
    public bool UseBusOutbox { get; set; } = true;
    public int QueryDelaySeconds { get; set; } = 3;
}
