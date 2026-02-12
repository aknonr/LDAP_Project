namespace API.Contracts.Realtime;

/// <summary>
/// SignalR ile yayinlanan target detay guncellemesi.
/// </summary>
public sealed class TargetUpdatedMessage
{
    public Guid JobId { get; set; }
    public Guid TargetId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? CorrelationId { get; set; }
}
