namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Job durum cevabi.
/// </summary>
public sealed record JobStatusOutput
{
    public Guid JobId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int TargetCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
}
