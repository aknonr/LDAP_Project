using Application.Abstractions.Update;
using Application.Models;
using Domain.Entities;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Update.Strategies;

/// <summary>
/// WinRM/PowerShell ile JSON sonuc donen update stratejileri icin taban sinif.
/// </summary>
public abstract class RemoteJsonUpdateStrategyBase : IUpdateStrategy
{
    private readonly IRemoteCommandExecutor _remoteCommandExecutor;
    private readonly ILogger _logger;

    protected RemoteJsonUpdateStrategyBase(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger logger)
    {
        _remoteCommandExecutor = remoteCommandExecutor;
        _logger = logger;
    }

    public abstract Domain.Enums.ResourceType ResourceType { get; }

    public async Task<OperationResult> UpdateAsync(
        UpdateContext context,
        JobResource resource,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resource.ResourceName))
        {
            return OperationResult.Failure("RESOURCE_NOT_FOUND", "Kaynak adi bos.");
        }

        var script = BuildScript(resource, context.TargetAccount, context.NewPassword);
        var remoteResult = await _remoteCommandExecutor.ExecutePowerShellAsync(
            context.ServerName,
            script,
            cancellationToken);

        if (!remoteResult.IsSuccess)
        {
            return OperationResult.Failure(
                remoteResult.ErrorCode ?? "WINRM_CONNECT_FAILED",
                remoteResult.ErrorMessage ?? "Uzaktan update komutu calistirilamadi.");
        }

        if (string.IsNullOrWhiteSpace(remoteResult.StandardOutput))
        {
            return OperationResult.Failure("UNKNOWN", "Update sonucu alinamadi.");
        }

        try
        {
            // Tüm stratejiler ortak state/code/message JSON formatı dondurur.
            using var document = JsonDocument.Parse(remoteResult.StandardOutput);
            var state = ReadString(document.RootElement, "state");
            var code = ReadString(document.RootElement, "code") ?? "UNKNOWN";
            var message = ReadString(document.RootElement, "message") ?? "Update sonucu belirsiz.";

            if (string.Equals(state, "UPDATED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "SKIPPED", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Update strategy sonucu basarili. Strategy={Strategy}, Server={Server}, Resource={Resource}, State={State}, Code={Code}",
                    GetType().Name,
                    context.ServerName,
                    resource.ResourceName,
                    state,
                    code);
                return OperationResult.Success();
            }

            _logger.LogWarning(
                "Update strategy sonucu basarisiz. Strategy={Strategy}, Server={Server}, Resource={Resource}, Code={Code}",
                GetType().Name,
                context.ServerName,
                resource.ResourceName,
                code);
            return OperationResult.Failure(code, message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Update strategy JSON parse hatasi. Strategy={Strategy}, Server={Server}, Resource={Resource}",
                GetType().Name,
                context.ServerName,
                resource.ResourceName);
            return OperationResult.Failure("UNKNOWN", "Update sonucu parse edilemedi.");
        }
    }

    protected abstract string BuildScript(JobResource resource, string targetAccount, string newPassword);

    protected static string ToBase64(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    protected static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }

    protected static string? GetIdentityFromResourcePath(string? resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        var separatorIndex = resourcePath.LastIndexOf('|');
        return separatorIndex >= 0
            ? resourcePath[(separatorIndex + 1)..]
            : resourcePath;
    }

    protected static string? GetIisAppPoolFromResourcePath(string? resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        var separatorIndex = resourcePath.IndexOf('|');
        if (separatorIndex <= 0)
        {
            return resourcePath;
        }

        return resourcePath[..separatorIndex];
    }
}
