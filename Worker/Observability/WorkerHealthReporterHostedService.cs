using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Worker.Configuration;

namespace Worker.Observability;

public sealed class WorkerHealthReporterHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<WorkerHealthOptions> _options;
    private readonly ILogger<WorkerHealthReporterHostedService> _logger;

    public WorkerHealthReporterHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<WorkerHealthOptions> options,
        ILogger<WorkerHealthReporterHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReportAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker health reporter dongusu hata verdi.");
            }

            var interval = TimeSpan.FromSeconds(Math.Max(15, _options.CurrentValue.IntervalSeconds));
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ReportAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return;
        }

        bool? dbConnected = null;
        if (options.CheckDatabase)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AdpmDbContext>();
            dbConnected = await dbContext.Database.CanConnectAsync(cancellationToken);
        }

        using var process = Process.GetCurrentProcess();
        var workingSetMb = process.WorkingSet64 / (1024d * 1024d);

        _logger.LogInformation(
            "Worker health. DbConnected={DbConnected}, WorkingSetMb={WorkingSetMb:0.0}, ThreadCount={ThreadCount}, HandleCount={HandleCount}",
            dbConnected,
            workingSetMb,
            process.Threads.Count,
            process.HandleCount);
    }
}
