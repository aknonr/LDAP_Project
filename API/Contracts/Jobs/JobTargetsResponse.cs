namespace API.Contracts.Jobs;

/// <summary>
/// Job hedef listesini dondurur.
/// </summary>
public sealed record JobTargetsResponse
{
    public Guid JobId { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyCollection<JobTargetDto> Targets { get; init; } = Array.Empty<JobTargetDto>();
}
