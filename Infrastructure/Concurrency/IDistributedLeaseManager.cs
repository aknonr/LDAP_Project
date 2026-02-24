namespace Infrastructure.Concurrency;

/// <summary>
/// DB tabanli lease kaydi ile singleton calisma garantisi saglar.
/// </summary>
public interface IDistributedLeaseManager
{
    Task<bool> TryAcquireAsync(
        string leaseName,
        string ownerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<bool> RenewAsync(
        string leaseName,
        string ownerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        string leaseName,
        string ownerId,
        CancellationToken cancellationToken);
}
