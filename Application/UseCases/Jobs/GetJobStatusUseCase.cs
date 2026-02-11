using Application.Abstractions.Repositories;
using Application.Models;
using Application.UseCases.Jobs.Models;
using Domain.Enums;

namespace Application.UseCases.Jobs;

/// <summary>
/// Job durumunu okur ve ozetler.
/// </summary>
public sealed class GetJobStatusUseCase
{
    private readonly IJobRepository _jobRepository;

    public GetJobStatusUseCase(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    /// <summary>
    /// Job bilgilerini ve basari/hatali hedef sayilarini dondurur.
    /// </summary>
    public async Task<UseCaseResult<JobStatusOutput>> ExecuteAsync(
        GetJobStatusInput input,
        CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(input.JobId, cancellationToken);
        if (job is null)
        {
            return UseCaseResult<JobStatusOutput>.Failure("JOB_NOT_FOUND", "Job bulunamadi.");
        }

        var totalCount = await _jobRepository.GetTargetCountAsync(job.Id, cancellationToken);
        var successCount = await _jobRepository.CountTargetsByStatusAsync(job.Id, TargetStatus.Success, cancellationToken);
        var failedCount = await _jobRepository.CountTargetsByStatusAsync(job.Id, TargetStatus.Failed, cancellationToken);

        return UseCaseResult<JobStatusOutput>.Success(new JobStatusOutput
        {
            JobId = job.Id,
            Type = job.Type.ToString(),
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            TargetCount = totalCount,
            SuccessCount = successCount,
            FailedCount = failedCount
        });
    }
}
