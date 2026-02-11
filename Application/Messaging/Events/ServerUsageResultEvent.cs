using Domain.Enums;

namespace Application.Messaging.Events;

/// <summary>
/// Discovery sonucunu bildiren event.
/// </summary>
public sealed record ServerUsageResultEvent
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public TargetStatus Status { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; init; }
}
