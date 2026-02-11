namespace API.Contracts.Jobs;

/// <summary>
/// Job icindeki bir hedefin durumunu tasir.
/// </summary>
public sealed record JobTargetDto
{
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
