using Application.Abstractions.Inventory;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ThApi;

public sealed class InventorySyncService : IInventorySyncService
{
    private readonly AdpmDbContext _dbContext;
    private readonly IThApiClient _thApiClient;
    private readonly IOptionsMonitor<ThApiOptions> _options;
    private readonly ILogger<InventorySyncService> _logger;

    public InventorySyncService(
        AdpmDbContext dbContext,
        IThApiClient thApiClient,
        IOptionsMonitor<ThApiOptions> options,
        ILogger<InventorySyncService> logger)
    {
        _dbContext = dbContext;
        _thApiClient = thApiClient;
        _options = options;
        _logger = logger;
    }

    public async Task<InventorySyncSummary> SyncAsync(CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return new InventorySyncSummary
            {
                IsEnabled = false,
                Success = true
            };
        }

        try
        {
            var records = await _thApiClient.GetInventoryAsync(cancellationToken);
            if (records.Count == 0)
            {
                return new InventorySyncSummary
                {
                    IsEnabled = true,
                    Success = true,
                    TotalRecords = 0
                };
            }

            var diffRule = ResolveDiffRule(options.DiffRule);
            var now = DateTimeOffset.UtcNow;

            var groupIds = records
                .Select(item => item.GroupId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingGroups = await _dbContext.ServerGroups
                .Where(group => groupIds.Contains(group.ExternalId))
                .ToListAsync(cancellationToken);

            var groupsByExternalId = existingGroups
                .ToDictionary(group => group.ExternalId, StringComparer.OrdinalIgnoreCase);

            var addedGroups = 0;
            var updatedGroups = 0;

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.GroupId))
                {
                    continue;
                }

                if (!groupsByExternalId.TryGetValue(record.GroupId, out var group))
                {
                    group = new ServerGroup
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = record.GroupId,
                        Name = record.GroupName
                    };
                    await _dbContext.ServerGroups.AddAsync(group, cancellationToken);
                    groupsByExternalId[record.GroupId] = group;
                    addedGroups++;
                }
                else if (!string.Equals(group.Name, record.GroupName, StringComparison.Ordinal))
                {
                    group.Name = record.GroupName;
                    updatedGroups++;
                }
            }

            var recordIds = records
                .Select(item => item.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingServers = await _dbContext.ServerInventories
                .Where(server => recordIds.Contains(server.ExternalId))
                .ToListAsync(cancellationToken);

            var serversByExternalId = existingServers
                .ToDictionary(server => server.ExternalId, StringComparer.OrdinalIgnoreCase);

            var addedServers = 0;
            var updatedServers = 0;
            var skippedServers = 0;

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Id) || string.IsNullOrWhiteSpace(record.Hostname))
                {
                    continue;
                }

                var targetGroup = ResolveGroup(groupsByExternalId, record.GroupId);
                var groupId = targetGroup?.Id;

                if (!serversByExternalId.TryGetValue(record.Id, out var server))
                {
                    server = new ServerInventory
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = record.Id,
                        Hostname = record.Hostname,
                        IpAddress = record.IpAddress,
                        ServerGroupId = groupId,
                        CreatedDate = record.CreatedDate ?? now,
                        UpdatedDate = record.UpdatedDate ?? record.CreatedDate ?? now
                    };
                    await _dbContext.ServerInventories.AddAsync(server, cancellationToken);
                    addedServers++;
                    continue;
                }

                var remoteTimestamp = ResolveRemoteTimestamp(diffRule, record);
                var localTimestamp = server.UpdatedDate ?? server.CreatedDate;
                var hasNewerTimestamp = remoteTimestamp.HasValue && remoteTimestamp > localTimestamp;

                var changed = hasNewerTimestamp
                              || !string.Equals(server.Hostname, record.Hostname, StringComparison.Ordinal)
                              || !string.Equals(server.IpAddress ?? string.Empty, record.IpAddress ?? string.Empty, StringComparison.Ordinal)
                              || server.ServerGroupId != groupId;

                if (!changed)
                {
                    skippedServers++;
                    continue;
                }

                server.Hostname = record.Hostname;
                server.IpAddress = record.IpAddress;
                server.ServerGroupId = groupId;
                server.UpdatedDate = remoteTimestamp ?? now;
                updatedServers++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new InventorySyncSummary
            {
                IsEnabled = true,
                Success = true,
                TotalRecords = records.Count,
                AddedGroups = addedGroups,
                UpdatedGroups = updatedGroups,
                AddedServers = addedServers,
                UpdatedServers = updatedServers,
                SkippedServers = skippedServers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory sync basarisiz.");
            return new InventorySyncSummary
            {
                IsEnabled = true,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static ServerGroup? ResolveGroup(
        IReadOnlyDictionary<string, ServerGroup> groups,
        string? externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        return groups.TryGetValue(externalId, out var group) ? group : null;
    }

    private static InventoryDiffRule ResolveDiffRule(string? rule)
    {
        if (string.Equals(rule, "CreatedDate", StringComparison.OrdinalIgnoreCase))
        {
            return InventoryDiffRule.CreatedDate;
        }

        return InventoryDiffRule.UpdatedDate;
    }

    private static DateTimeOffset? ResolveRemoteTimestamp(InventoryDiffRule rule, ThInventoryRecord record)
    {
        return rule == InventoryDiffRule.CreatedDate
            ? record.CreatedDate ?? record.UpdatedDate
            : record.UpdatedDate ?? record.CreatedDate;
    }

    private enum InventoryDiffRule
    {
        CreatedDate,
        UpdatedDate
    }
}
