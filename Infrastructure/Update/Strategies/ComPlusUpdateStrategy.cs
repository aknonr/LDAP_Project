using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Update.Strategies;

/// <summary>
/// COM+ application identity hedef hesapla eslesiyorsa sifreyi gunceller.
/// </summary>
public sealed class ComPlusUpdateStrategy : RemoteJsonUpdateStrategyBase
{
    public ComPlusUpdateStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<ComPlusUpdateStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.COMPlus;

    protected override string BuildScript(Domain.Entities.JobResource resource, string targetAccount, string newPassword)
    {
        var appNameBase64 = ToBase64(resource.ResourceName);
        var targetAccountBase64 = ToBase64(targetAccount);
        var newPasswordBase64 = ToBase64(newPassword);

        return $$"""
$appName = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{appNameBase64}}'))
$targetAccount = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{targetAccountBase64}}'))
$newPassword = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{newPasswordBase64}}'))

try {
    $catalog = New-Object -ComObject COMAdmin.COMAdminCatalog
    $apps = $catalog.GetCollection('Applications')
    $apps.Populate()

    $app = $null
    foreach ($candidate in $apps) {
        if ($candidate.Name -eq $appName) {
            $app = $candidate
            break
        }
    }

    if (-not $app) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'COM+ uygulamasi bulunamadi.' } | ConvertTo-Json -Compress
        return
    }

    $currentIdentity = $app.Value('Identity')
    if ([string]::IsNullOrWhiteSpace($currentIdentity)) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'COM+ identity bilgisi bos.' } | ConvertTo-Json -Compress
        return
    }

    if (-not $currentIdentity.Equals($targetAccount, [System.StringComparison]::OrdinalIgnoreCase)) {
        [PSCustomObject]@{ state = 'SKIPPED'; code = 'IDEMPOTENT_SKIP'; message = 'COM+ hedef hesapla eslesmiyor.' } | ConvertTo-Json -Compress
        return
    }

    $app.Value('Identity') = $targetAccount
    $app.Value('Password') = $newPassword
    $apps.SaveChanges()

    [PSCustomObject]@{ state = 'UPDATED'; code = 'OK'; message = 'COM+ credentials guncellendi.' } | ConvertTo-Json -Compress
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
