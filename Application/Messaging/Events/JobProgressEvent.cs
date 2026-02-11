using Domain.Enums;

namespace Application.Messaging.Events;

/// <summary>
/// Job genel ilerleme durumunu bildiren event.
/// </summary>
public sealed record JobProgressEvent
{
    public Guid JobId { get; init; }
    public JobStatus Status { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; init; }
}
