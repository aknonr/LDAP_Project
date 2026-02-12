namespace Application.Abstractions.Inventory;

/// <summary>
/// Inventory sync sonuc ozeti.
/// </summary>
public sealed record InventorySyncSummary
{
    public bool IsEnabled { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int TotalRecords { get; init; }
    public int AddedGroups { get; init; }
    public int UpdatedGroups { get; init; }
    public int AddedServers { get; init; }
    public int UpdatedServers { get; init; }
    public int SkippedServers { get; init; }
}
