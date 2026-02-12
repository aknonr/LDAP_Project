namespace Infrastructure.Messaging;

/// <summary>
/// RabbitMQ baglanti ayarlari (TLS dahil).
/// </summary>
public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5671;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public bool UseTls { get; set; } = true;
    public string? SslServerName { get; set; }
    public ushort RequestedHeartbeat { get; set; } = 30;
    public bool UseQuorumQueues { get; set; } = true;
    public int? QuorumReplicationFactor { get; set; } = null;
}
