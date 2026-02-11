using Application.Abstractions.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ServerGroupRepository : IServerGroupRepository
{
    private readonly AdpmDbContext _dbContext;

    public ServerGroupRepository(AdpmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ServerGroup?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        // Dis kaynak grup kimligi ile tek grup getirir.
        return _dbContext.ServerGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<ServerInventory>> GetServersAsync(Guid serverGroupId, CancellationToken cancellationToken)
    {
        // Islenecek hedefleri inventory tablosundan ceker.
        return await _dbContext.ServerInventories
            .AsNoTracking()
            .Where(server => server.ServerGroupId == serverGroupId)
            .OrderBy(server => server.Hostname)
            .ToListAsync(cancellationToken);
    }
}
