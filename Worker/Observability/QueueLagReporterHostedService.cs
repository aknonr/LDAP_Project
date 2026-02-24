using Microsoft.Extensions.Options;
using Worker.Configuration;

namespace Worker.Observability;

public sealed class QueueLagReporterHostedService : BackgroundService
{
    private readonly IQueueLagProbe _queueLagProbe;
    private readonly IOptionsMonitor<QueueLagOptions> _options;
    private readonly ILogger<QueueLagReporterHostedService> _logger;

    public QueueLagReporterHostedService(
        IQueueLagProbe queueLagProbe,
        IOptionsMonitor<QueueLagOptions> options,
        ILogger<QueueLagReporterHostedService> logger)
    {
        _queueLagProbe = queueLagProbe;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReportQueueLagAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue lag reporter dongusu hata verdi.");
            }

            var intervalSeconds = Math.Max(10, _options.CurrentValue.IntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task ReportQueueLagAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled || options.Queues.Count == 0)
        {
            return;
        }

        var readyThreshold = Math.Max(1, options.WarningReadyThreshold);
        var unackedThreshold = Math.Max(1, options.WarningUnackedThreshold);
        var virtualHost = string.IsNullOrWhiteSpace(options.VirtualHost) ? "/" : options.VirtualHost.Trim();
        foreach (var queueName in options.Queues.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var sample = await _queueLagProbe.TryReadAsync(queueName.Trim(), virtualHost, cancellationToken);
            if (sample is null)
            {
                continue;
            }

            var shouldWarn = sample.MessagesReady >= readyThreshold || sample.MessagesUnacknowledged >= unackedThreshold;
            if (shouldWarn)
            {
                _logger.LogWarning(
                    "Queue lag threshold asildi. Queue={Queue}, Ready={MessagesReady}, Unacked={MessagesUnacked}, ThresholdReady={ThresholdReady}, ThresholdUnacked={ThresholdUnacked}",
                    sample.QueueName,
                    sample.MessagesReady,
                    sample.MessagesUnacknowledged,
                    readyThreshold,
                    unackedThreshold);
                continue;
            }

            _logger.LogInformation(
                "Queue lag metric. Queue={Queue}, Ready={MessagesReady}, Unacked={MessagesUnacked}",
                sample.QueueName,
                sample.MessagesReady,
                sample.MessagesUnacknowledged);
        }
    }
}
