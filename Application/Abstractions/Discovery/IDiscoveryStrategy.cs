using Domain.Enums;

namespace Application.Abstractions.Discovery;

/// <summary>
/// Kaynak tipine gore discovery stratejisi.
/// </summary>
public interface IDiscoveryStrategy
{
    ResourceType ResourceType { get; }

    /// <summary>
    /// Hedef sunucuda kaynak tespiti yapar.
    /// </summary>
    Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken);
}
