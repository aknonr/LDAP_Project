namespace Application.Abstractions.Inventory;

/// <summary>
/// Inventory senkronizasyonunu yurutur.
/// </summary>
public interface IInventorySyncService
{
    /// <summary>
    /// TH API ile DB arasindaki inventory verisini senkronlar.
    /// </summary>
    Task<InventorySyncSummary> SyncAsync(CancellationToken cancellationToken);
}
