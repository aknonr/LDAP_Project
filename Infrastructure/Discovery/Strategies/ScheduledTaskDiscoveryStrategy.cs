using Application.Abstractions.Discovery;
using Domain.Enums;
using Infrastructure.RemoteExecution;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Discovery.Strategies;

public sealed class ScheduledTaskDiscoveryStrategy : RemoteJsonDiscoveryStrategyBase
{
    public ScheduledTaskDiscoveryStrategy(
        IRemoteCommandExecutor remoteCommandExecutor,
        ILogger<ScheduledTaskDiscoveryStrategy> logger)
        : base(remoteCommandExecutor, logger)
    {
    }

    public override ResourceType ResourceType => ResourceType.ScheduledTask;

    protected override string BuildScript()
    {
        return @"
$builtIns = @('SYSTEM','LOCAL SERVICE','NETWORK SERVICE','NT AUTHORITY\SYSTEM','NT AUTHORITY\LOCAL SERVICE','NT AUTHORITY\NETWORK SERVICE')
Get-ScheduledTask |
    Where-Object { $_.Principal -and $_.Principal.UserId -and ($builtIns -notcontains $_.Principal.UserId.ToUpperInvariant()) } |
    ForEach-Object {
        [PSCustomObject]@{
            TaskPath = $_.TaskPath
            TaskName = $_.TaskName
            UserId = $_.Principal.UserId
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
        var taskName = ReadString(element, "TaskName");
        var taskPath = ReadString(element, "TaskPath");
        var userId = ReadString(element, "UserId");

        if (string.IsNullOrWhiteSpace(taskName))
        {
            return;
        }

        resources.Add(new DiscoveredResource
        {
            ResourceType = ResourceType.ScheduledTask,
            ResourceName = $"{taskPath}{taskName}",
            ResourcePath = userId
        });
    }
}
