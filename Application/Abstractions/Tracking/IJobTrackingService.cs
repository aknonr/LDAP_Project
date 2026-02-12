using Application.Abstractions.Tracking.Models;
using Domain.Enums;

namespace Application.Abstractions.Tracking;

/// <summary>
/// Result event'leri ile job/target durumunu gunceller.
/// </summary>
public interface IJobTrackingService
{
    /// <summary>
    /// Target sonucunu uygular ve guncel ozeti dondurur.
    /// </summary>
    Task<TargetUpdateSnapshot?> ApplyTargetResultAsync(
        Guid jobId,
        Guid targetId,
        TargetStatus status,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset occurredAt,
        string? correlationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Job seviyesindeki ilerleme bilgisini uygular.
    /// </summary>
    Task<JobProgressSnapshot?> ApplyJobProgressAsync(
        Guid jobId,
        JobStatus status,
        string? message,
        DateTimeOffset occurredAt,
        string? correlationId,
        CancellationToken cancellationToken);
}
