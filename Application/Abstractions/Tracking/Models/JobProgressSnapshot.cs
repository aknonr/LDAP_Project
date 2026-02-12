namespace Application.Abstractions.Tracking.Models;

/// <summary>
/// Job genel durum guncellemesi sonrasi UI ve izleme ozeti.
/// </summary>
public sealed record JobProgressSnapshot
{
    public Guid JobId { get; init; }
    public string JobStatus { get; init; } = string.Empty;
    public string? Message { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int TargetCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public string? CorrelationId { get; init; }
}
