namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Job hedef listesi cevabi.
/// </summary>
public sealed record JobTargetsOutput
{
    public Guid JobId { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyCollection<JobTargetItem> Targets { get; init; } = Array.Empty<JobTargetItem>();
}
