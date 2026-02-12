namespace API.Contracts.Realtime;

/// <summary>
/// SignalR ile yayinlanan job ozet guncellemesi.
/// </summary>
public sealed class JobUpdatedMessage
{
    public Guid JobId { get; set; }
    public string JobStatus { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int TargetCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string? CorrelationId { get; set; }
}
