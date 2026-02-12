namespace Application.Abstractions.Tracking.Models;

/// <summary>
/// Hedef guncellemesi sonrasi UI ve izleme icin ozet durum.
/// </summary>
public sealed record TargetUpdateSnapshot
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public string TargetStatus { get; init; } = string.Empty;
    public string JobStatus { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public int TargetCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public string? CorrelationId { get; init; }
}
