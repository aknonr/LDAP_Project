using Application.Abstractions.Tracking;
using Application.Abstractions.Tracking.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Tracking;

public sealed class JobTrackingService : IJobTrackingService
{
    private readonly AdpmDbContext _dbContext;

    public JobTrackingService(AdpmDbContext dbContext)
    {
        _dbContext = dbContext;
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

        target.Status = status;
        target.ErrorCode = errorCode;
        target.ErrorMessage = SensitiveDataRedactor.Redact(errorMessage);
        target.UpdatedAt = NormalizeTimestamp(occurredAt);

        if (job.StartedAt is null)
        {
            job.StartedAt = target.UpdatedAt;
        }

        job.CorrelationId ??= correlationId;

        var counts = await BuildCountsAsync(jobId, cancellationToken);
        ApplyJobState(job, counts.PendingCount, counts.InProgressCount, counts.FailedCount, target.UpdatedAt);

        await _dbContext.SaveChangesAsync(cancellationToken);

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

        return new JobProgressSnapshot
        {
            JobId = job.Id,
            JobStatus = job.Status.ToString(),
            Message = SensitiveDataRedactor.Redact(message),
            UpdatedAt = normalizedTimestamp,
            TargetCount = counts.TotalCount,
            SuccessCount = counts.SuccessCount,
            FailedCount = counts.FailedCount,
            CorrelationId = correlationId ?? job.CorrelationId
        };
    }

    private static void ApplyJobState(Job job, int pendingCount, int inProgressCount, int failedCount, DateTimeOffset updatedAt)
    {
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
