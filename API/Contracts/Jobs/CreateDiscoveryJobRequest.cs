namespace API.Contracts.Jobs;

/// <summary>
/// Discovery job baslatmak icin kullanilir.
/// </summary>
public sealed record CreateDiscoveryJobRequest
{
    public string ServerGroupId { get; init; } = string.Empty;
    public string? TicketRef { get; init; }
}
