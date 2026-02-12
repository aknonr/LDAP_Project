namespace Application.Abstractions.Inventory;

/// <summary>
/// Inventory API uzerinden inventory bilgisi cekmek icin sozlesme.
/// </summary>
public interface IInventoryApiClient
{
    /// <summary>
    /// Inventory kayitlarini getirir.
    /// </summary>
    Task<IReadOnlyList<InventoryApiRecord>> GetInventoryAsync(CancellationToken cancellationToken);
}
