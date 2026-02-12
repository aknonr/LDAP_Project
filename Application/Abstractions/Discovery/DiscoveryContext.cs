namespace Application.Abstractions.Discovery;

/// <summary>
/// Discovery istegi icin temel baglam.
/// </summary>
public sealed record DiscoveryContext
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
}
