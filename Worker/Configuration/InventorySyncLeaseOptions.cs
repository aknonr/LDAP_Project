namespace Worker.Configuration;

/// <summary>
/// Inventory sync singleton calisma ayarlari.
/// </summary>
public sealed class InventorySyncLeaseOptions
{
    public bool Enabled { get; set; } = true;
    public string LeaseName { get; set; } = "inventory-sync";
    public int LeaseDurationSeconds { get; set; } = 300;
    public int RenewIntervalSeconds { get; set; } = 60;
    public int AcquisitionRetrySeconds { get; set; } = 2;
}
