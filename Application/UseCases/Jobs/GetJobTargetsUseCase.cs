using Application.Abstractions.Repositories;
using Application.Models;
using Application.UseCases.Jobs.Models;

namespace Application.UseCases.Jobs;

/// <summary>
/// Job hedef listesini getirir.
/// </summary>
public sealed class GetJobTargetsUseCase
{
    private readonly IJobRepository _jobRepository;

    public GetJobTargetsUseCase(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    /// <summary>
    /// Job hedeflerini sayfalayarak dondurur.
    /// </summary>
    public async Task<UseCaseResult<JobTargetsOutput>> ExecuteAsync(
        GetJobTargetsInput input,
        CancellationToken cancellationToken)
    {
        if (input.Take <= 0 || input.Take > 2000)
        {
            return UseCaseResult<JobTargetsOutput>.Failure("INVALID_TAKE", "Take 1-2000 araliginda olmalidir.");
        }

        var job = await _jobRepository.GetByIdAsync(input.JobId, cancellationToken);
        if (job is null)
        {
            return UseCaseResult<JobTargetsOutput>.Failure("JOB_NOT_FOUND", "Job bulunamadi.");
        }

        var totalCount = await _jobRepository.GetTargetCountAsync(input.JobId, cancellationToken);
        var targets = await _jobRepository.GetTargetsAsync(input.JobId, input.Skip, input.Take, cancellationToken);

        var items = targets.Select(target => new JobTargetItem
        {
            TargetId = target.Id,
            ServerName = target.ServerName,
            Status = target.Status.ToString(),
            ErrorCode = target.ErrorCode,
            ErrorMessage = target.ErrorMessage,
            UpdatedAt = target.UpdatedAt
        }).ToList();

        return UseCaseResult<JobTargetsOutput>.Success(new JobTargetsOutput
        {
            JobId = input.JobId,
            TotalCount = totalCount,
            Targets = items
        });
    }
}
