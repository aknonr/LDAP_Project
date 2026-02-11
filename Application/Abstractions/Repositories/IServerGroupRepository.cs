using Domain.Entities;

namespace Application.Abstractions.Repositories;

/// <summary>
/// ServerGroup ve ServerInventory erisimi icin sozlesme.
/// </summary>
public interface IServerGroupRepository
{
    /// <summary>
    /// Dis kaynak ID (ExternalId) ile server grubu getirir.
    /// </summary>
    Task<ServerGroup?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>
    /// Bir gruba bagli sunuculari getirir.
    /// </summary>
    Task<IReadOnlyList<ServerInventory>> GetServersAsync(Guid serverGroupId, CancellationToken cancellationToken);
}
