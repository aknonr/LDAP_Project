using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Update.Strategies;

/// <summary>
/// ScheduledTask principal'i hedef hesapla eslesiyorsa sifreyi gunceller.
/// </summary>
public sealed class ScheduledTaskUpdateStrategy : RemoteJsonUpdateStrategyBase
{
    public ScheduledTaskUpdateStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<ScheduledTaskUpdateStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.ScheduledTask;

    protected override string BuildScript(Domain.Entities.JobResource resource, string targetAccount, string newPassword)
    {
        var taskNameBase64 = ToBase64(resource.ResourceName);
        var targetAccountBase64 = ToBase64(targetAccount);
        var newPasswordBase64 = ToBase64(newPassword);

        return $$"""
$taskFullName = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{taskNameBase64}}'))
$targetAccount = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{targetAccountBase64}}'))
$newPassword = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{{newPasswordBase64}}'))

try {
    $taskPath = '\'
    $taskName = $taskFullName
    if ($taskFullName.Contains('\')) {
        $separator = $taskFullName.LastIndexOf('\')
        $taskPath = $taskFullName.Substring(0, $separator + 1)
        $taskName = $taskFullName.Substring($separator + 1)
    }

    $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath -ErrorAction SilentlyContinue
    if (-not $task) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'Scheduled task bulunamadi.' } | ConvertTo-Json -Compress
        return
    }

    $currentUser = $task.Principal.UserId
    if ([string]::IsNullOrWhiteSpace($currentUser)) {
        [PSCustomObject]@{ state = 'FAILED'; code = 'RESOURCE_NOT_FOUND'; message = 'Scheduled task principal bilgisi bos.' } | ConvertTo-Json -Compress
        return
    }

    if (-not $currentUser.Equals($targetAccount, [System.StringComparison]::OrdinalIgnoreCase)) {
        [PSCustomObject]@{ state = 'SKIPPED'; code = 'IDEMPOTENT_SKIP'; message = 'Scheduled task hedef hesapla eslesmiyor.' } | ConvertTo-Json -Compress
        return
    }

    Set-ScheduledTask -TaskName $taskName -TaskPath $taskPath -User $targetAccount -Password $newPassword -ErrorAction Stop | Out-Null
    [PSCustomObject]@{ state = 'UPDATED'; code = 'OK'; message = 'Scheduled task credentials guncellendi.' } | ConvertTo-Json -Compress
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
