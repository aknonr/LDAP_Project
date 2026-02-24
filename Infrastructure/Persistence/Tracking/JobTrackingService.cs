using Application.Abstractions.Tracking;
using Application.Abstractions.Tracking.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Tracking;

public sealed class JobTrackingService : IJobTrackingService
{
    private readonly AdpmDbContext _dbContext;
    private readonly ILogger<JobTrackingService> _logger;

    public JobTrackingService(AdpmDbContext dbContext, ILogger<JobTrackingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TargetUpdateSnapshot?> ApplyTargetResultAsync(
        Guid jobId,
        Guid targetId,
        TargetStatus status,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset occurredAt,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        // Result event'i ile hedef kaydini gunceller ve job ozetini tekrar hesaplar.
        var normalizedTimestamp = NormalizeTimestamp(occurredAt);
        var target = await _dbContext.JobTargets
            .FirstOrDefaultAsync(item => item.JobId == jobId && item.Id == targetId, cancellationToken);

        if (target is null)
        {
            return null;
        }

        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        if (job is null)
        {
            return null;
        }

        if (!CanApplyTargetTransition(target.Status, status, target.UpdatedAt, normalizedTimestamp))
        {
            _logger.LogWarning(
                "Stale/out-of-order target event drop. JobId={JobId}, TargetId={TargetId}, Current={CurrentStatus}, Incoming={IncomingStatus}, CurrentAt={CurrentUpdatedAt}, IncomingAt={IncomingUpdatedAt}",
                jobId,
                targetId,
                target.Status,
                status,
                target.UpdatedAt,
                normalizedTimestamp);

            var staleCounts = await BuildCountsAsync(jobId, cancellationToken);
            return BuildTargetSnapshot(target, job, staleCounts, correlationId);
        }

        target.Status = status;
        target.ErrorCode = errorCode;
        target.ErrorMessage = SensitiveDataRedactor.Redact(errorMessage);
        target.UpdatedAt = normalizedTimestamp;

        if (job.StartedAt is null)
        {
            job.StartedAt = target.UpdatedAt;
        }

        job.CorrelationId ??= correlationId;

        var counts = await BuildCountsAsync(jobId, cancellationToken);
        ApplyJobState(job, counts.PendingCount, counts.InProgressCount, counts.FailedCount, target.UpdatedAt);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildTargetSnapshot(target, job, counts, correlationId);
    }

    public async Task<JobProgressSnapshot?> ApplyJobProgressAsync(
        Guid jobId,
        JobStatus status,
        string? message,
        DateTimeOffset occurredAt,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        // Job seviyesindeki event'i uygular ve canli guncelleme icin ozet dondurur.
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        if (job is null)
        {
            return null;
        }

        var normalizedTimestamp = NormalizeTimestamp(occurredAt);
        if (!CanApplyJobTransition(job, status, normalizedTimestamp))
        {
            _logger.LogWarning(
                "Stale/out-of-order job progress event drop. JobId={JobId}, Current={CurrentStatus}, Incoming={IncomingStatus}, IncomingAt={IncomingUpdatedAt}",
                job.Id,
                job.Status,
                status,
                normalizedTimestamp);

            var staleCounts = await BuildCountsAsync(job.Id, cancellationToken);
            return BuildJobSnapshot(job, staleCounts, message, ResolveJobLastKnownTimestamp(job), correlationId);
        }

        job.Status = status;
        job.CorrelationId ??= correlationId;

        if (status == JobStatus.InProgress && job.StartedAt is null)
        {
            job.StartedAt = normalizedTimestamp;
            job.CompletedAt = null;
        }

        if (IsTerminal(status))
        {
            job.CompletedAt = normalizedTimestamp;
        }

        if (status == JobStatus.Pending)
        {
            job.CompletedAt = null;
        }

        var counts = await BuildCountsAsync(job.Id, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildJobSnapshot(job, counts, message, normalizedTimestamp, correlationId);
    }

    private static TargetUpdateSnapshot BuildTargetSnapshot(
        JobTarget target,
        Job job,
        (int TotalCount, int PendingCount, int InProgressCount, int SuccessCount, int FailedCount) counts,
        string? correlationId)
    {
        return new TargetUpdateSnapshot
        {
            JobId = job.Id,
            TargetId = target.Id,
            TargetStatus = target.Status.ToString(),
            JobStatus = job.Status.ToString(),
            ErrorCode = target.ErrorCode,
            ErrorMessage = target.ErrorMessage,
            UpdatedAt = target.UpdatedAt,
            TargetCount = counts.TotalCount,
            SuccessCount = counts.SuccessCount,
            FailedCount = counts.FailedCount,
            CorrelationId = correlationId ?? job.CorrelationId
        };
    }

    private static JobProgressSnapshot BuildJobSnapshot(
        Job job,
        (int TotalCount, int PendingCount, int InProgressCount, int SuccessCount, int FailedCount) counts,
        string? message,
        DateTimeOffset updatedAt,
        string? correlationId)
    {
        return new JobProgressSnapshot
        {
            JobId = job.Id,
            JobStatus = job.Status.ToString(),
            Message = SensitiveDataRedactor.Redact(message),
            UpdatedAt = updatedAt,
            TargetCount = counts.TotalCount,
            SuccessCount = counts.SuccessCount,
            FailedCount = counts.FailedCount,
            CorrelationId = correlationId ?? job.CorrelationId
        };
    }

    private static bool CanApplyTargetTransition(
        TargetStatus current,
        TargetStatus incoming,
        DateTimeOffset currentUpdatedAt,
        DateTimeOffset incomingUpdatedAt)
    {
        if (incomingUpdatedAt < currentUpdatedAt)
        {
            return false;
        }

        if (current == incoming)
        {
            return true;
        }

        if (IsTerminalTargetStatus(current))
        {
            return false;
        }

        if (incoming == TargetStatus.Pending && current != TargetStatus.Pending)
        {
            return false;
        }

        return true;
    }

    private static bool CanApplyJobTransition(Job job, JobStatus incoming, DateTimeOffset incomingTimestamp)
    {
        var lastKnownTimestamp = ResolveJobLastKnownTimestamp(job);
        if (incomingTimestamp < lastKnownTimestamp && incoming != job.Status)
        {
            return false;
        }

        if (job.Status == incoming)
        {
            return true;
        }

        if (IsTerminal(job.Status))
        {
            return false;
        }

        if (incoming == JobStatus.Pending && job.Status != JobStatus.Pending)
        {
            return false;
        }

        return true;
    }

    private static DateTimeOffset ResolveJobLastKnownTimestamp(Job job)
    {
        if (job.CompletedAt is { } completedAt)
        {
            return completedAt;
        }

        if (job.StartedAt is { } startedAt)
        {
            return startedAt;
        }

        return job.CreatedAt == default ? DateTimeOffset.UtcNow : job.CreatedAt;
    }

    private static bool IsTerminalTargetStatus(TargetStatus status)
    {
        return status is TargetStatus.Success or TargetStatus.Failed or TargetStatus.Skipped;
    }

    private static void ApplyJobState(Job job, int pendingCount, int inProgressCount, int failedCount, DateTimeOffset updatedAt)
    {
        if (IsTerminal(job.Status))
        {
            return;
        }

        if (pendingCount > 0 || inProgressCount > 0)
        {
            job.Status = JobStatus.InProgress;
            job.CompletedAt = null;
            return;
        }

        job.Status = failedCount > 0 ? JobStatus.Failed : JobStatus.Success;
        job.CompletedAt = updatedAt;
    }

    private async Task<(int TotalCount, int PendingCount, int InProgressCount, int SuccessCount, int FailedCount)> BuildCountsAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.JobTargets
            .AsNoTracking()
            .Where(item => item.JobId == jobId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                PendingCount = group.Sum(item => item.Status == TargetStatus.Pending ? 1 : 0),
                InProgressCount = group.Sum(item => item.Status == TargetStatus.InProgress ? 1 : 0),
                SuccessCount = group.Sum(item => item.Status == TargetStatus.Success ? 1 : 0),
                FailedCount = group.Sum(item => item.Status == TargetStatus.Failed ? 1 : 0)
            })
            .Select(item => ValueTuple.Create(
                item.TotalCount,
                item.PendingCount,
                item.InProgressCount,
                item.SuccessCount,
                item.FailedCount))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsTerminal(JobStatus status)
    {
        return status is JobStatus.Success or JobStatus.Failed or JobStatus.Cancelled;
    }

    private static DateTimeOffset NormalizeTimestamp(DateTimeOffset value)
    {
        return value == default ? DateTimeOffset.UtcNow : value;
    }
}
