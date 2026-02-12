using Application.Abstractions.Update;
using Application.Models;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Update;

public sealed class UpdateEngine : IUpdateEngine
{
    private readonly AdpmDbContext _dbContext;
    private readonly IReadOnlyCollection<IUpdateStrategy> _strategies;
    private readonly ILogger<UpdateEngine> _logger;

    public UpdateEngine(
        AdpmDbContext dbContext,
        IEnumerable<IUpdateStrategy> strategies,
        ILogger<UpdateEngine> logger)
    {
        _dbContext = dbContext;
        _strategies = strategies.ToList();
        _logger = logger;
    }

    public async Task<UpdateResult> UpdateAsync(UpdateContext context, CancellationToken cancellationToken)
    {
        var target = await _dbContext.JobTargets
            .Include(item => item.Resources)
            .FirstOrDefaultAsync(item => item.JobId == context.JobId && item.Id == context.TargetId, cancellationToken);

        if (target is null)
        {
            return Failure("RESOURCE_NOT_FOUND", "Job target bulunamadi.");
        }

        if (target.Resources.Count == 0)
        {
            return await FailAndUpdateTarget(target, "RESOURCE_NOT_FOUND", "Target icin kaynak bulunamadi.", cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        string? targetErrorCode = null;
        string? targetErrorMessage = null;

        foreach (var resource in target.Resources)
        {
            if (resource.Status == TargetStatus.Success)
            {
                continue;
            }

            var strategy = _strategies.FirstOrDefault(item => item.ResourceType == resource.ResourceType);
            if (strategy is null)
            {
                resource.Status = TargetStatus.Failed;
                resource.ErrorCode = "RESOURCE_NOT_FOUND";
                resource.ErrorMessage = "Update strategy bulunamadi.";
                resource.UpdatedAt = now;
                targetErrorCode ??= resource.ErrorCode;
                targetErrorMessage ??= resource.ErrorMessage;
                continue;
            }

            _logger.LogInformation(
                "Update strategy basladi. JobId={JobId}, TargetId={TargetId}, ResourceId={ResourceId}, ResourceType={ResourceType}, ResourceName={ResourceName}",
                context.JobId,
                context.TargetId,
                resource.Id,
                resource.ResourceType,
                resource.ResourceName);

            OperationResult result;
            try
            {
                result = await strategy.UpdateAsync(context, resource, cancellationToken);
            }
            catch (OperationFailureException ex)
            {
                result = OperationResult.Failure(ex.ErrorCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update strategy hata verdi. TargetId={TargetId}, ResourceId={ResourceId}", target.Id, resource.Id);
                result = OperationResult.Failure("UNKNOWN", "Update islemi hata verdi.");
            }

            if (result.IsSuccess)
            {
                resource.Status = TargetStatus.Success;
                resource.ErrorCode = null;
                resource.ErrorMessage = null;
                _logger.LogInformation(
                    "Update strategy basarili. JobId={JobId}, TargetId={TargetId}, ResourceId={ResourceId}",
                    context.JobId,
                    context.TargetId,
                    resource.Id);
            }
            else
            {
                resource.Status = TargetStatus.Failed;
                resource.ErrorCode = result.ErrorCode ?? "UNKNOWN";
                resource.ErrorMessage = SensitiveDataRedactor.Redact(result.ErrorMessage);
                targetErrorCode ??= resource.ErrorCode;
                targetErrorMessage ??= resource.ErrorMessage;
                _logger.LogWarning(
                    "Update strategy basarisiz. JobId={JobId}, TargetId={TargetId}, ResourceId={ResourceId}, ErrorCode={ErrorCode}",
                    context.JobId,
                    context.TargetId,
                    resource.Id,
                    resource.ErrorCode);
            }

            resource.UpdatedAt = now;
        }

        target.Status = target.Resources.Any(item => item.Status == TargetStatus.Failed)
            ? TargetStatus.Failed
            : TargetStatus.Success;
        target.ErrorCode = targetErrorCode;
        target.ErrorMessage = SensitiveDataRedactor.Redact(targetErrorMessage);
        target.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateResult
        {
            Status = target.Status,
            ErrorCode = target.ErrorCode,
            ErrorMessage = target.ErrorMessage
        };
    }

    private UpdateResult Failure(string errorCode, string errorMessage)
    {
        return new UpdateResult
        {
            Status = TargetStatus.Failed,
            ErrorCode = errorCode,
            ErrorMessage = SensitiveDataRedactor.Redact(errorMessage)
        };
    }

    private async Task<UpdateResult> FailAndUpdateTarget(
        Domain.Entities.JobTarget target,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        target.Status = TargetStatus.Failed;
        target.ErrorCode = errorCode;
        target.ErrorMessage = SensitiveDataRedactor.Redact(errorMessage);
        target.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Failure(errorCode, errorMessage);
    }
}
