using Application.Abstractions.Discovery;
using Domain.Enums;

namespace Infrastructure.Discovery.Strategies;

public sealed class UserRightDiscoveryStrategy : IDiscoveryStrategy
{
    public ResourceType ResourceType => ResourceType.UserRight;

    public Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken)
    {
        // User right atamalarini bulma burada uygulanacak.
        return Task.FromResult<IReadOnlyList<DiscoveredResource>>(Array.Empty<DiscoveredResource>());
    }
}
