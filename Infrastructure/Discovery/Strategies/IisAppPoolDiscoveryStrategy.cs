using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class IisAppPoolDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public IisAppPoolDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<IisAppPoolDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.IISAppPool;

    protected override string BuildScript()
    {
        return @"
Import-Module WebAdministration -ErrorAction Stop
Get-ChildItem IIS:\AppPools |
    ForEach-Object {
        $identityType = $_.processModel.identityType
        $userName = if ($identityType -eq 3 -or $identityType -eq 'SpecificUser') { $_.processModel.userName } else { $null }
        if ($userName) {
            [PSCustomObject]@{
                Name = $_.Name
                UserName = $userName
            }
        }
    } |
    ConvertTo-Json -Compress
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
        var userName = ReadString(element, "UserName");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.IISAppPool,
            ResourceName = name,
            ResourcePath = userName
        });
    }
}
