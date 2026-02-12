using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Update.Strategies;

public sealed class ServiceUpdateStrategy : IUpdateStrategy
{
    private readonly IRemoteCommandExecutor _remoteCommandExecutor;
    private readonly ILogger<ServiceUpdateStrategy> _logger;

    public ServiceUpdateStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<ServiceUpdateStrategy> logger)
    {
        _remoteCommandExecutor = remoteCommandExecutor;
        _logger = logger;
    }

    public ResourceType ResourceType => ResourceType.Service;

    public async Task<OperationResult> UpdateAsync(
        UpdateContext context,
        Domain.Entities.JobResource resource,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resource.ResourceName))
        {
            return OperationResult.Failure("RESOURCE_NOT_FOUND", "Service kaydi gecersiz.");
        }

        var script = BuildScript(
            resource.ResourceName,
            context.TargetAccount,
            context.NewPassword);

        var remoteResult = await _remoteCommandExecutor.ExecutePowerShellAsync(
            context.ServerName,
            script,
            cancellationToken);

        if (!remoteResult.IsSuccess)
        {
            return OperationResult.Failure(
                remoteResult.ErrorCode ?? "WINRM_CONNECT_FAILED",
                remoteResult.ErrorMessage ?? "Service update uzaktan calistirilamadi.");
        }

        if (string.IsNullOrWhiteSpace(remoteResult.StandardOutput))
        {
            return OperationResult.Failure("UNKNOWN", "Service update sonucu alinamadi.");
        }

        try
        {
            // Remote script sonucu tekil JSON obje olarak doner; state/code/message okunur.
            using var document = JsonDocument.Parse(remoteResult.StandardOutput);
            var state = ReadString(document.RootElement, "state");
            var code = ReadString(document.RootElement, "code") ?? "UNKNOWN";
            var message = ReadString(document.RootElement, "message") ?? "Service update sonucu belirsiz.";

            if (string.Equals(state, "UPDATED", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Service update basarili. Server={Server}, Service={Service}",
                    context.ServerName,
                    resource.ResourceName);
                return OperationResult.Success();
            }

            if (string.Equals(state, "SKIPPED", StringComparison.OrdinalIgnoreCase))
            {
                // Idempotent davranis: hesap eslesmiyorsa basarili kabul edilir.
                _logger.LogInformation(
                    "Service update idempotent skip. Server={Server}, Service={Service}, Reason={Reason}",
                    context.ServerName,
                    resource.ResourceName,
                    code);
                return OperationResult.Success();
            }

            _logger.LogWarning(
                "Service update basarisiz. Server={Server}, Service={Service}, ErrorCode={ErrorCode}",
                context.ServerName,
                resource.ResourceName,
                code);
            return OperationResult.Failure(code, message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Service update JSON parse hatasi. Server={Server}, Service={Service}", context.ServerName, resource.ResourceName);
            return OperationResult.Failure("UNKNOWN", "Service update JSON parse basarisiz.");
        }
    }

    private static string BuildScript(string serviceName, string targetAccount, string newPassword)
    {
        // Hassas veri script'e base64 ile enjekte edilir; dogrudan quote birlestirme yapilmaz.
        var serviceNameBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(serviceName));
        var targetAccountBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(targetAccount));
        var newPasswordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(newPassword));

        return $$"""
$serviceName = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{serviceNameBase64}}'))
$targetAccount = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{targetAccountBase64}}'))
$newPassword = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{newPasswordBase64}}'))

try {
    $safeServiceName = $serviceName.Replace("'", "''")
    $svc = Get-CimInstance Win32_Service -Filter ("Name='{0}'" -f $safeServiceName)
    if (-not $svc) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'Service bulunamadi.' } | ConvertTo-Json -Compress
        return
    }

    if (-not $svc.StartName) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'Service StartName bos.' } | ConvertTo-Json -Compress
        return
    }

    if (-not $svc.StartName.Equals($targetAccount, [System.StringComparison]::OrdinalIgnoreCase)) {
        [PSCustomObject]@{ state = 'SKIPPED'; code = 'IDEMPOTENT_SKIP'; message = 'Service hedef hesapla eslesmiyor.' } | ConvertTo-Json -Compress
        return
    }

    $changeResult = Invoke-CimMethod -InputObject $svc -MethodName Change -Arguments @{
        StartName = $targetAccount
        StartPassword = $newPassword
    }

    switch ($changeResult.ReturnValue) {
        0 { [PSCustomObject]@{ state = 'UPDATED'; code = 'OK'; message = 'Service credential guncellendi.' } | ConvertTo-Json -Compress; return }
        2 { [PSCustomObject]@{ state = 'FAILED'; code = 'ACCESS_DENIED'; message = 'Erisim reddedildi.' } | ConvertTo-Json -Compress; return }
        7 { [PSCustomObject]@{ state = 'FAILED'; code = 'TIMEOUT'; message = 'Service islemi timeout.' } | ConvertTo-Json -Compress; return }
        15 { [PSCustomObject]@{ state = 'FAILED'; code = 'ACCESS_DENIED'; message = 'Logon hatasi.' } | ConvertTo-Json -Compress; return }
        22 { [PSCustomObject]@{ state = 'FAILED'; code = 'ACCESS_DENIED'; message = 'Gecersiz service account.' } | ConvertTo-Json -Compress; return }
        Default {
            [PSCustomObject]@{ state = 'FAILED'; code = 'UNKNOWN'; message = ('Service Change hata kodu: ' + $changeResult.ReturnValue) } | ConvertTo-Json -Compress
            return
        }
    }
}
catch {
    [PSCustomObject]@{ state = 'FAILED'; code = 'UNKNOWN'; message = $_.Exception.Message } | ConvertTo-Json -Compress
}
""";
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }
}
