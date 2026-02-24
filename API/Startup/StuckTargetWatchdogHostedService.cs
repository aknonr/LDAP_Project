using Application.Abstractions.Tracking;
using Domain.Enums;
using Infrastructure.Concurrency;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Startup;

/// <summary>
/// Uzun sure InProgress kalan hedefleri periyodik tarar.
/// Varsayilan mod sadece gozlem/log; opsiyonel olarak hedefleri timeout-fail'e ceker.
/// </summary>
public sealed class StuckTargetWatchdogHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<StuckTargetWatchdogOptions> _options;
    private readonly ILogger<StuckTargetWatchdogHostedService> _logger;
    private readonly string _leaseOwnerId;

    public StuckTargetWatchdogHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<StuckTargetWatchdogOptions> options,
        ILogger<StuckTargetWatchdogHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _leaseOwnerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stuck target watchdog dongusu hata verdi.");
            }

            var intervalSeconds = Math.Max(10, _options.CurrentValue.ScanIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var leaseManager = scope.ServiceProvider.GetRequiredService<IDistributedLeaseManager>();
        var shouldReleaseLease = false;
        var leaseName = string.IsNullOrWhiteSpace(options.LeaseName)
            ? "stuck-target-watchdog"
            : options.LeaseName.Trim();

        if (options.UseDistributedLease)
        {
            var leaseDuration = TimeSpan.FromSeconds(Math.Max(30, options.LeaseDurationSeconds));
            var acquired = await leaseManager.TryAcquireAsync(
                leaseName,
                _leaseOwnerId,
                leaseDuration,
                cancellationToken);

            if (!acquired)
            {
                return;
            }

            shouldReleaseLease = true;
        }

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AdpmDbContext>();
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-Math.Max(60, options.TargetInProgressTimeoutSeconds));
            var batchSize = Math.Max(1, options.BatchSize);

            var staleTargets = await dbContext.JobTargets
                .AsNoTracking()
                .Where(item => item.Status == TargetStatus.InProgress && item.UpdatedAt < cutoff)
                .OrderBy(item => item.UpdatedAt)
                .Take(batchSize)
                .Select(item => new
                {
                    item.JobId,
                    TargetId = item.Id,
                    item.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            if (staleTargets.Count == 0)
            {
                return;
            }

            _logger.LogWarning(
                "Stuck target tespit edildi. Count={Count}, CutoffUtc={CutoffUtc}, AutoFail={AutoFail}",
                staleTargets.Count,
                cutoff,
                options.AutoFailTimedOutTargets);

            if (!options.AutoFailTimedOutTargets)
            {
                return;
            }

            var trackingService = scope.ServiceProvider.GetRequiredService<IJobTrackingService>();
            var now = DateTimeOffset.UtcNow;
            foreach (var target in staleTargets)
            {
                await trackingService.ApplyTargetResultAsync(
                    target.JobId,
                    target.TargetId,
                    TargetStatus.Failed,
                    "TARGET_STUCK_TIMEOUT",
                    "Hedef uzun sure InProgress durumda kaldi (watchdog timeout).",
                    now,
                    null,
                    cancellationToken);
            }
        }
        finally
        {
            if (shouldReleaseLease)
            {
                try
                {
                    await leaseManager.ReleaseAsync(leaseName, _leaseOwnerId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Stuck watchdog lease release hatasi. Lease={LeaseName}",
                        leaseName);
                }
            }
        }
    }
}
