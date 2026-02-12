using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class IisVirtualDirDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public IisVirtualDirDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<IisVirtualDirDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.IISVirtualDir;

    protected override string BuildScript()
    {
        return @"
Import-Module WebAdministration -ErrorAction Stop

$appPoolUsers = @{}
Get-ChildItem IIS:\AppPools | ForEach-Object {
    $identityType = $_.processModel.identityType
    $userName = if ($identityType -eq 3 -or $identityType -eq 'SpecificUser') { $_.processModel.userName } else { $null }
    $appPoolUsers[$_.Name] = $userName
}

Get-WebVirtualDirectory | ForEach-Object {
    $siteName = $_.PSPath.Split('\')[-4]
    $appPath = $_.Application
    $webApp = Get-WebApplication -Site $siteName | Where-Object { $_.Path -eq $appPath } | Select-Object -First 1
    $appPool = if ($webApp) { $webApp.ApplicationPool } else { $null }
    $identity = if ($appPool) { $appPoolUsers[$appPool] } else { $null }
    if ($identity) {
        [PSCustomObject]@{
            Site = $siteName
            Application = $appPath
            Path = $_.Path
            PhysicalPath = $_.PhysicalPath
            ApplicationPool = $appPool
            Identity = $identity
        }
    }
} | ConvertTo-Json -Compress
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
        var site = ReadString(element, "Site");
        var app = ReadString(element, "Application");
        var path = ReadString(element, "Path");
        var appPool = ReadString(element, "ApplicationPool");
        var identity = ReadString(element, "Identity");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.IISVirtualDir,
            ResourceName = $"{site}:{app}:{path}",
            ResourcePath = $"{appPool}|{identity}"
        });
    }
}
