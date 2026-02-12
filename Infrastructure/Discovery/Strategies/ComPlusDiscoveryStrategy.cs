using Application.Abstractions.Discovery;
using Domain.Enums;

namespace Infrastructure.Discovery.Strategies;

public sealed class ComPlusDiscoveryStrategy : IDiscoveryStrategy
{
    public ResourceType ResourceType => ResourceType.COMPlus;

    public Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken)
    {
        // COM+ hesaplarini bulma burada uygulanacak.
        return Task.FromResult<IReadOnlyList<DiscoveredResource>>(Array.Empty<DiscoveredResource>());
    }
}
