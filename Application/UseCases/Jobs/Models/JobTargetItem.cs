namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Job hedef detayi.
/// </summary>
public sealed record JobTargetItem
{
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
