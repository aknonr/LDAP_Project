using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class ServiceDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public ServiceDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<ServiceDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.Service;

    protected override string BuildScript()
    {
        return @"
$builtIns = @('LocalSystem','LocalService','NetworkService','NT AUTHORITY\LocalService','NT AUTHORITY\NetworkService')
Get-CimInstance Win32_Service |
    Where-Object { $_.StartName -and ($builtIns -notcontains $_.StartName) } |
    Select-Object Name, StartName |
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
        var serviceName = ReadString(element, "Name");
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return;
        }

        var startName = ReadString(element, "StartName");
        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.Service,
            ResourceName = serviceName,
            ResourcePath = startName
        });
    }
}
