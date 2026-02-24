namespace Worker.Configuration;

/// <summary>
/// RabbitMQ queue backlog gozlemi ayarlari.
/// </summary>
public sealed class QueueLagOptions
{
    public bool Enabled { get; set; } = false;
    public string ManagementBaseUrl { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 10;
    public int WarningReadyThreshold { get; set; } = 500;
    public int WarningUnackedThreshold { get; set; } = 500;
    public List<string> Queues { get; set; } = [];
}
