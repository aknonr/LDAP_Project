using Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Concurrency;

public sealed class SqlDistributedLeaseManager : IDistributedLeaseManager
{
    private readonly AdpmDbContext _dbContext;
    private readonly ILogger<SqlDistributedLeaseManager> _logger;

    public SqlDistributedLeaseManager(
        AdpmDbContext dbContext,
        ILogger<SqlDistributedLeaseManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(
        string leaseName,
        string ownerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leaseName) || string.IsNullOrWhiteSpace(ownerId))
        {
            return false;
        }

        var normalizedDuration = NormalizeDuration(leaseDuration);
        var now = DateTimeOffset.UtcNow;
        var newLeaseUntil = now.Add(normalizedDuration);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var lease = await _dbContext.DistributedLeases
                .FirstOrDefaultAsync(item => item.Name == leaseName, cancellationToken);

            if (lease is null)
            {
                _dbContext.DistributedLeases.Add(new Domain.Entities.DistributedLease
                {
                    Name = leaseName,
                    OwnerId = ownerId,
                    LeaseUntilUtc = newLeaseUntil,
                    AcquiredAtUtc = now,
                    RenewedAtUtc = now
                });

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return true;
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    _logger.LogDebug(
                        ex,
                        "Lease race algilandi (insert). Lease={LeaseName}, Owner={OwnerId}, Attempt={Attempt}",
                        leaseName,
                        ownerId,
                        attempt);
                    _dbContext.ChangeTracker.Clear();
                    continue;
                }
            }

            if (lease.LeaseUntilUtc > now && !string.Equals(lease.OwnerId, ownerId, StringComparison.Ordinal))
            {
                return false;
            }

            lease.OwnerId = ownerId;
            lease.LeaseUntilUtc = newLeaseUntil;
            lease.RenewedAtUtc = now;
            if (lease.AcquiredAtUtc == default)
            {
                lease.AcquiredAtUtc = now;
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Lease race algilandi (concurrency). Lease={LeaseName}, Owner={OwnerId}, Attempt={Attempt}",
                    leaseName,
                    ownerId,
                    attempt);
                _dbContext.ChangeTracker.Clear();
            }
        }

        return false;
    }

    public async Task<bool> RenewAsync(
        string leaseName,
        string ownerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leaseName) || string.IsNullOrWhiteSpace(ownerId))
        {
            return false;
        }

        var lease = await _dbContext.DistributedLeases
            .FirstOrDefaultAsync(item => item.Name == leaseName, cancellationToken);

        if (lease is null || !string.Equals(lease.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (lease.LeaseUntilUtc < now)
        {
            return false;
        }

        lease.LeaseUntilUtc = now.Add(NormalizeDuration(leaseDuration));
        lease.RenewedAtUtc = now;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogDebug(
                ex,
                "Lease yenileme yarisi algilandi. Lease={LeaseName}, Owner={OwnerId}",
                leaseName,
                ownerId);
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task ReleaseAsync(
        string leaseName,
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leaseName) || string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        var lease = await _dbContext.DistributedLeases
            .FirstOrDefaultAsync(item => item.Name == leaseName, cancellationToken);

        if (lease is null || !string.Equals(lease.OwnerId, ownerId, StringComparison.Ordinal))
        {
            return;
        }

        _dbContext.DistributedLeases.Remove(lease);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Lease baska instance tarafindan degismis/temizlenmis olabilir.
        }
    }

    private static TimeSpan NormalizeDuration(TimeSpan leaseDuration)
    {
        return leaseDuration < TimeSpan.FromSeconds(30)
            ? TimeSpan.FromSeconds(30)
            : leaseDuration;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
               && sqlException.Number is 2601 or 2627;
    }
}
