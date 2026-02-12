using Application.Abstractions.Inventory;
using Infrastructure.ThApi;
using Microsoft.Extensions.Options;

namespace Worker.Jobs;

/// <summary>
/// TH API inventory senkronizasyonunu periyodik calistirir.
/// </summary>
public sealed class InventorySyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ThApiOptions> _options;
    private readonly ILogger<InventorySyncJob> _logger;

    public InventorySyncJob(
        IServiceProvider serviceProvider,
        IOptionsMonitor<ThApiOptions> options,
        ILogger<InventorySyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
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
}
