namespace Application.Abstractions.Inventory;

/// <summary>
/// TH API uzerinden inventory bilgisi cekmek icin sozlesme.
/// </summary>
public interface IThApiClient
{
    /// <summary>
    /// Inventory kayitlarini getirir.
    /// </summary>
    Task<IReadOnlyList<ThInventoryRecord>> GetInventoryAsync(CancellationToken cancellationToken);
}
