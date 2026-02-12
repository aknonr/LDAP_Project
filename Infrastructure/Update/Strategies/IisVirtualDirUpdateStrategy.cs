using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Update.Strategies;

/// <summary>
/// IIS VirtualDirectory ilgili webapp/appool identity hedef hesapla eslesiyorsa sifreyi gunceller.
/// </summary>
public sealed class IisVirtualDirUpdateStrategy : RemoteJsonUpdateStrategyBase
{
    public IisVirtualDirUpdateStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<IisVirtualDirUpdateStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.IISVirtualDir;

    protected override string BuildScript(Domain.Entities.JobResource resource, string targetAccount, string newPassword)
    {
        var appPoolName = GetIisAppPoolFromResourcePath(resource.ResourcePath);
        if (string.IsNullOrWhiteSpace(appPoolName))
        {
            return """
[PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'IIS virtual directory apppool bilgisi bulunamadi.' } | ConvertTo-Json -Compress
""";
        }

        var appPoolNameBase64 = ToBase64(appPoolName);
        var targetAccountBase64 = ToBase64(targetAccount);
        var newPasswordBase64 = ToBase64(newPassword);

        return $$"""
$appPoolName = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{appPoolNameBase64}}'))
$targetAccount = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{targetAccountBase64}}'))
$newPassword = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{newPasswordBase64}}'))

try {
    Import-Module WebAdministration -ErrorAction Stop

    $pool = Get-Item ("IIS:\AppPools\" + $appPoolName) -ErrorAction SilentlyContinue
    if (-not $pool) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'IIS AppPool bulunamadi.' } | ConvertTo-Json -Compress
        return
    }

    $currentUser = $pool.processModel.userName
    if ([string]::IsNullOrWhiteSpace($currentUser)) {
        [PSCustomObject]@{ state = 'SKIPPED'; code = 'IDEMPOTENT_SKIP'; message = 'AppPool specific user ile calismiyor.' } | ConvertTo-Json -Compress
        return
    }

    if (-not $currentUser.Equals($targetAccount, [System.StringComparison]::OrdinalIgnoreCase)) {
        [PSCustomObject]@{ state = 'SKIPPED'; code = 'IDEMPOTENT_SKIP'; message = 'AppPool hedef hesapla eslesmiyor.' } | ConvertTo-Json -Compress
        return
    }

    $pool.processModel.identityType = 3
    $pool.processModel.userName = $targetAccount
    $pool.processModel.password = $newPassword
    $pool | Set-Item -ErrorAction Stop

    [PSCustomObject]@{ state = 'UPDATED'; code = 'OK'; message = 'IIS virtual directory apppool credentials guncellendi.' } | ConvertTo-Json -Compress
}
catch {
    $msg = $_.Exception.Message
    if ($msg -match 'Access is denied|permission') {
        [PSCustomObject]@{ state = 'FAILED'; code = 'ACCESS_DENIED'; message = $msg } | ConvertTo-Json -Compress
        return
    }

    if ($msg -match 'cannot find|not found|bulunam') {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = $msg } | ConvertTo-Json -Compress
        return
    }

    [PSCustomObject]@{ state = 'FAILED'; code = 'UNKNOWN'; message = $msg } | ConvertTo-Json -Compress
}
""";
    }
}
