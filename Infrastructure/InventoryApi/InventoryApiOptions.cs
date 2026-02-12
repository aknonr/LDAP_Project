namespace Infrastructure.InventoryApi;

/// <summary>
/// Inventory API entegrasyon ayarlari.
/// </summary>
public sealed class InventoryApiOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = string.Empty;
    public string InventoryEndpoint { get; set; } = "/inventory";
    public string? ApiKey { get; set; }
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public string? BearerToken { get; set; }
    public int TimeoutSeconds { get; set; } = 15;
    public string DiffRule { get; set; } = "UpdatedDate";
    public int SyncIntervalSeconds { get; set; } = 900;
    public int InitialDelaySeconds { get; set; } = 10;
}
