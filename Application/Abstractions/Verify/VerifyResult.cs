using Domain.Enums;

namespace Application.Abstractions.Verify;

/// <summary>
/// Verify sonucu hedef seviyesinde dondurulur.
/// </summary>
public sealed record VerifyResult
{
    public TargetStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
