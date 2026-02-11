namespace Application.Messaging.Commands;

/// <summary>
/// Hedef sunucuda servis hesabi kullanimini kesfetmek icin gonderilen komut.
/// </summary>
public sealed record DiscoverServerUsageCommand
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
