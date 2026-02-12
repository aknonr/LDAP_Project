using Application.Abstractions.Discovery;
using Application.Models;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

/// <summary>
/// WinRM/PowerShell uzerinden JSON sonuc donen discovery stratejileri icin taban sinif.
/// </summary>
public abstract class RemoteJsonDiscoveryStrategyBase : IDiscoveryStrategy
{
    private readonly IRemoteCommandExecutor _remoteCommandExecutor;
    private readonly ILogger _logger;

    protected RemoteJsonDiscoveryStrategyBase(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger logger)
    {
        _remoteCommandExecutor = remoteCommandExecutor;
        _logger = logger;
    }

    public abstract Domain.Enums.ResourceType ResourceType { get; }

    public async Task<IReadOnlyList<DiscoveredResource>> DiscoverAsync(
        DiscoveryContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Discovery strategy basladi. Strategy={Strategy}, JobId={JobId}, TargetId={TargetId}, Server={Server}",
            GetType().Name,
            context.JobId,
            context.TargetId,
            context.ServerName);

        var remoteResult = await _remoteCommandExecutor.ExecutePowerShellAsync(
            context.ServerName,
            BuildScript(),
            cancellationToken);

        if (!remoteResult.IsSuccess)
        {
            throw new OperationFailureException(
                remoteResult.ErrorCode ?? "WINRM_CONNECT_FAILED",
                remoteResult.ErrorMessage ?? "Remote discovery komutu basarisiz.");
        }

        if (string.IsNullOrWhiteSpace(remoteResult.StandardOutput))
        {
            _logger.LogInformation(
                "Discovery strategy sonuc donmedi. Strategy={Strategy}, Server={Server}",
                GetType().Name,
                context.ServerName);
            return Array.Empty<DiscoveredResource>();
        }

        try
        {
            // Tum remote stratejiler JSON uretir; parsing tek noktadan standartlastirilir.
            using var document = JsonDocument.Parse(remoteResult.StandardOutput);
            var resources = ParseResources(document.RootElement);
            stopwatch.Stop();

            _logger.LogInformation(
                "Discovery strategy tamamlandi. Strategy={Strategy}, Count={Count}, DurationMs={DurationMs}",
                GetType().Name,
                resources.Count,
                stopwatch.ElapsedMilliseconds);

            return resources;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Discovery JSON parse hatasi. Strategy={Strategy}", GetType().Name);
            throw new OperationFailureException("UNKNOWN", "Discovery JSON parse basarisiz.");
        }
    }

    protected abstract string BuildScript();

    protected abstract IReadOnlyList<DiscoveredResource> ParseResources(JsonElement rootElement);

    protected static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : prop.ToString();
    }
}
