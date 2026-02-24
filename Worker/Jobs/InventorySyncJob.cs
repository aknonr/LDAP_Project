using Application.Abstractions.Inventory;
using Infrastructure.Concurrency;
using Infrastructure.ThApi;
using Microsoft.Extensions.Options;
using Worker.Configuration;

namespace Worker.Jobs;

/// <summary>
/// TH API inventory senkronizasyonunu periyodik calistirir.
/// </summary>
public sealed class InventorySyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ThApiOptions> _options;
    private readonly IOptionsMonitor<InventorySyncLeaseOptions> _leaseOptions;
    private readonly ILogger<InventorySyncJob> _logger;
    private readonly string _ownerId;

    public InventorySyncJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<ThApiOptions> options,
        IOptionsMonitor<InventorySyncLeaseOptions> leaseOptions,
        ILogger<InventorySyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _leaseOptions = leaseOptions;
        _logger = logger;
        _ownerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialDelay = TimeSpan.FromSeconds(Math.Max(0, _options.CurrentValue.InitialDelaySeconds));
        if (initialDelay > TimeSpan.Zero)
        {
            await Task.Delay(initialDelay, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            var interval = TimeSpan.FromSeconds(Math.Max(60, _options.CurrentValue.SyncIntervalSeconds));
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IInventorySyncService>();
        var leaseManager = scope.ServiceProvider.GetRequiredService<IDistributedLeaseManager>();

        var leaseConfig = _leaseOptions.CurrentValue;
        var leaseDuration = TimeSpan.FromSeconds(Math.Max(30, leaseConfig.LeaseDurationSeconds));
        var renewInterval = TimeSpan.FromSeconds(Math.Max(5, leaseConfig.RenewIntervalSeconds));

        if (!leaseConfig.Enabled)
        {
            await RunSyncAndLogAsync(syncService, cancellationToken);
            return;
        }

        var leaseName = string.IsNullOrWhiteSpace(leaseConfig.LeaseName)
            ? "inventory-sync"
            : leaseConfig.LeaseName.Trim();

        var acquired = await leaseManager.TryAcquireAsync(
            leaseName,
            _ownerId,
            leaseDuration,
            cancellationToken);

        if (!acquired && leaseConfig.AcquisitionRetrySeconds > 0)
        {
            var retryDelay = TimeSpan.FromSeconds(Math.Max(1, leaseConfig.AcquisitionRetrySeconds));
            await Task.Delay(retryDelay, cancellationToken);
            acquired = await leaseManager.TryAcquireAsync(
                leaseName,
                _ownerId,
                leaseDuration,
                cancellationToken);
        }

        if (!acquired)
        {
            _logger.LogInformation(
                "Inventory sync atlandi. Lease baska instance'da. Lease={LeaseName}",
                leaseName);
            return;
        }

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renewTask = MaintainLeaseAsync(
            leaseManager,
            leaseName,
            _ownerId,
            leaseDuration,
            renewInterval,
            runCts,
            cancellationToken);

        try
        {
            await RunSyncAndLogAsync(syncService, runCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Inventory sync iptal edildi. Lease kaybi algilandi. Lease={LeaseName}",
                leaseName);
        }
        finally
        {
            runCts.Cancel();
            await AwaitRenewLoopAsync(renewTask);

            try
            {
                await leaseManager.ReleaseAsync(leaseName, _ownerId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Inventory lease release hatasi. Lease={LeaseName}",
                    leaseName);
            }
        }
    }

    private async Task RunSyncAndLogAsync(IInventorySyncService syncService, CancellationToken cancellationToken)
    {
        var summary = await syncService.SyncAsync(cancellationToken);
        if (!summary.IsEnabled)
        {
            _logger.LogInformation("Inventory sync devre disi.");
            return;
        }

        if (!summary.Success)
        {
            _logger.LogWarning("Inventory sync basarisiz. Hata={Error}", summary.ErrorMessage);
            return;
        }

        _logger.LogInformation(
            "Inventory sync tamamlandi. Total={Total} AddedGroups={AddedGroups} UpdatedGroups={UpdatedGroups} AddedServers={AddedServers} UpdatedServers={UpdatedServers} Skipped={Skipped}",
            summary.TotalRecords,
            summary.AddedGroups,
            summary.UpdatedGroups,
            summary.AddedServers,
            summary.UpdatedServers,
            summary.SkippedServers);
    }

    private async Task MaintainLeaseAsync(
        IDistributedLeaseManager leaseManager,
        string leaseName,
        string ownerId,
        TimeSpan leaseDuration,
        TimeSpan renewInterval,
        CancellationTokenSource runCts,
        CancellationToken stoppingToken)
    {
        using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, runCts.Token);

        while (!delayCts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(renewInterval, delayCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var renewed = await leaseManager.RenewAsync(
                leaseName,
                ownerId,
                leaseDuration,
                delayCts.Token);

            if (renewed)
            {
                continue;
            }

            _logger.LogWarning(
                "Inventory lease yenileme basarisiz. Islem durduruluyor. Lease={LeaseName}",
                leaseName);
            runCts.Cancel();
            return;
        }
    }

    private static async Task AwaitRenewLoopAsync(Task renewTask)
    {
        try
        {
            await renewTask;
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
    }
}
