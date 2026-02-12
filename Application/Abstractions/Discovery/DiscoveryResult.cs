using Domain.Enums;

namespace Application.Abstractions.Discovery;

/// <summary>
/// Discovery sonuc ozeti.
/// </summary>
public sealed record DiscoveryResult
{
    public TargetStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<DiscoveredResource> Resources { get; init; } = Array.Empty<DiscoveredResource>();
}
