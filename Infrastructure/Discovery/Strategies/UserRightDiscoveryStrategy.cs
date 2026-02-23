using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class UserRightDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public UserRightDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<UserRightDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.UserRight;

    protected override string BuildScript()
    {
        // NOTE: Script remote server'da calisir. Sadece JSON yazmalidir (baska stdout parse'i bozar).
        return @"
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Resolve-SidToName([string]$sid) {
    try {
        $sidObj = New-Object System.Security.Principal.SecurityIdentifier($sid)
        $nt = $sidObj.Translate([System.Security.Principal.NTAccount])
        return $nt.Value
    } catch {
        return $sid
    }
}

$tempFile = Join-Path $env:TEMP ([Guid]::NewGuid().ToString() + '.inf')
$lines = $null
try {
    & secedit.exe /export /areas USER_RIGHTS /cfg $tempFile | Out-Null
    if ($LASTEXITCODE -ne 0) { throw ('secedit export failed. ExitCode=' + $LASTEXITCODE) }
    $lines = Get-Content -Path $tempFile -ErrorAction Stop
}
finally {
    Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue | Out-Null
}

$inSection = $false
$result = New-Object System.Collections.Generic.List[object]
$skipSids = @('S-1-5-18','S-1-5-19','S-1-5-20')

foreach ($line in $lines) {
    if ($line -match '^\\[Privilege Rights\\]\\s*$') {
        $inSection = $true
        continue
    }

    if ($inSection -and $line -match '^\\[') {
        break
    }

    if (-not $inSection) {
        continue
    }

    if ($line -match '^\\s*([^=\\s]+)\\s*=\\s*(.*)$') {
        $right = $matches[1].Trim()
        $accountsRaw = $matches[2].Trim()
        if ([string]::IsNullOrWhiteSpace($right) -or [string]::IsNullOrWhiteSpace($accountsRaw)) {
            continue
        }

        $accounts = $accountsRaw.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        foreach ($a in $accounts) {
            $sid = $a.TrimStart('*')
            if ($skipSids -contains $sid) { continue }

            $accountName = Resolve-SidToName $sid
            $result.Add([PSCustomObject]@{
                RightName = $right
                Account = $accountName
                Sid = $sid
            }) | Out-Null
        }
    }
}

$json = $result | ConvertTo-Json -Compress
if ([string]::IsNullOrWhiteSpace($json)) { $json = '[]' }
Write-Output $json
";
    }

    protected override IReadOnlyList<DiscoveredResource> ParseResources(JsonElement rootElement)
    {
        var resources = new List<DiscoveredResource>();

        if (rootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in rootElement.EnumerateArray())
            {
                TryAdd(resources, element);
            }
        }
        else if (rootElement.ValueKind == JsonValueKind.Object)
        {
            TryAdd(resources, rootElement);
        }

        return resources;
    }

    private static void TryAdd(List<DiscoveredResource> resources, JsonElement element)
    {
        var rightName = ReadString(element, "RightName");
        var account = ReadString(element, "Account");

        if (string.IsNullOrWhiteSpace(rightName) || string.IsNullOrWhiteSpace(account))
        {
            return;
        }

        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.UserRight,
            ResourceName = rightName,
            ResourcePath = account
        });
    }
}
