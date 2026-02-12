using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class ComPlusDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public ComPlusDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<ComPlusDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.COMPlus;

    protected override string BuildScript()
    {
        return @"
$builtIns = @('NT AUTHORITY\NETWORK SERVICE','NT AUTHORITY\LOCAL SERVICE','LOCAL SYSTEM','SYSTEM')
$catalog = New-Object -ComObject COMAdmin.COMAdminCatalog
$apps = $catalog.GetCollection('Applications')
$apps.Populate()

$result = @()
foreach ($app in $apps) {
    $identity = $app.Value('Identity')
    if ($identity -and ($builtIns -notcontains $identity.ToUpperInvariant())) {
        $result += [PSCustomObject]@{
            Name = $app.Name
            Identity = $identity
        }
    }
}

$result | ConvertTo-Json -Compress
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
        var identity = ReadString(element, "Identity");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.COMPlus,
            ResourceName = name,
            ResourcePath = identity
        });
    }
}
