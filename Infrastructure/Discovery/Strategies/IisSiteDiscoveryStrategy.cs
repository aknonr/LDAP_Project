using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class IisSiteDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public IisSiteDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<IisSiteDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.IISSite;

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

Get-Website | ForEach-Object {
    $ap = $_.ApplicationPool
    $identity = $appPoolUsers[$ap]
    if ($identity) {
        [PSCustomObject]@{
            Name = $_.Name
            ApplicationPool = $ap
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
        var name = ReadString(element, "Name");
        var appPool = ReadString(element, "ApplicationPool");
        var identity = ReadString(element, "Identity");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.IISSite,
            ResourceName = name,
            ResourcePath = $"{appPool}|{identity}"
        });
    }
}
