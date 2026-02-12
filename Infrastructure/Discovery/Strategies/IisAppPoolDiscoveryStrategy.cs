using Application.Abstractions.Discovery;
using Domain.Enums;

namespace Infrastructure.Discovery.Strategies;

public sealed class IisAppPoolDiscoveryStrategy : IDiscoveryStrategy
{
    public ResourceType ResourceType => ResourceType.IISAppPool;

    public Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken)
    {
        // IIS AppPool hesaplarini bulma burada uygulanacak.
        return Task.FromResult<IReadOnlyList<DiscoveredResource>>(Array.Empty<DiscoveredResource>());
    }
}
