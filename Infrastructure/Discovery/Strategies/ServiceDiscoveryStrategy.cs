using Application.Abstractions.Discovery;
using Domain.Enums;

namespace Infrastructure.Discovery.Strategies;

public sealed class ServiceDiscoveryStrategy : IDiscoveryStrategy
{
    public ResourceType ResourceType => ResourceType.Service;

    public Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken)
    {
        // WinRM/PowerShell ile servis hesaplarini bulma burada uygulanacak.
        return Task.FromResult<IReadOnlyList<DiscoveredResource>>(Array.Empty<DiscoveredResource>());
    }
}
