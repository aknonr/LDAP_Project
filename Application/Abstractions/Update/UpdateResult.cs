using Domain.Enums;

namespace Application.Abstractions.Update;

/// <summary>
/// Update sonuc ozeti.
/// </summary>
public sealed record UpdateResult
{
    public TargetStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
