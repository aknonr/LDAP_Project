namespace Application.Abstractions.Update;

/// <summary>
/// Update istegi icin temel baglam.
/// </summary>
public sealed record UpdateContext
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string TargetAccount { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}
