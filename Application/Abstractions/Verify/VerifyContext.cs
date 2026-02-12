namespace Application.Abstractions.Verify;

/// <summary>
/// Verify islemi icin gerekli hedef baglami.
/// </summary>
public sealed record VerifyContext
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string TargetAccount { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}
