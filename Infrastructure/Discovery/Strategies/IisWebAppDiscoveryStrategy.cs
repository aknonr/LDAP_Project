using Application.Abstractions.Discovery;
using Domain.Enums;

namespace Infrastructure.Discovery.Strategies;

public sealed class IisWebAppDiscoveryStrategy : IDiscoveryStrategy
{
    public ResourceType ResourceType => ResourceType.IISWebApp;

    public Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken)
    {
        // IIS web app hesaplarini bulma burada uygulanacak.
        return Task.FromResult<IReadOnlyList<DiscoveredResource>>(Array.Empty<DiscoveredResource>());
    }
}
