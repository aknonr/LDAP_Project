namespace Application.Abstractions.Discovery;

/// <summary>
/// Tum discovery stratejilerini orkestre eder.
/// </summary>
public interface IDiscoveryEngine
{
    /// <summary>
    /// Discovery islemini calistirir.
    /// </summary>
    Task<DiscoveryResult> DiscoverAsync(DiscoveryContext context, CancellationToken cancellationToken);
}
