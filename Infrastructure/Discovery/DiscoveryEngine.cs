using Application.Abstractions.Discovery;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Discovery;

public sealed class DiscoveryEngine : IDiscoveryEngine
{
    private readonly AdpmDbContext _dbContext;
    private readonly IReadOnlyCollection<IDiscoveryStrategy> _strategies;
    private readonly ILogger<DiscoveryEngine> _logger;

    public DiscoveryEngine(
        AdpmDbContext dbContext,
        IEnumerable<IDiscoveryStrategy> strategies,
        ILogger<DiscoveryEngine> logger)
    {
        _dbContext = dbContext;
        _strategies = strategies.ToList();
        _logger = logger;
    }

    public async Task<DiscoveryResult> DiscoverAsync(DiscoveryContext context, CancellationToken cancellationToken)
    {
        var target = await _dbContext.JobTargets
            .Include(item => item.Resources)
            .FirstOrDefaultAsync(item => item.JobId == context.JobId && item.Id == context.TargetId, cancellationToken);

        if (target is null)
        {
            return Failure("TARGET_NOT_FOUND", "Job target bulunamadi.");
        }

        if (_strategies.Count == 0)
        {
            return await FailAndUpdate(target, "NOT_IMPLEMENTED", "Discovery engine henuz entegre edilmedi.", cancellationToken);
        }

        try
        {
            var resources = new List<DiscoveredResource>();
            foreach (var strategy in _strategies)
            {
                var discovered = await strategy.DiscoverAsync(context, cancellationToken);
                if (discovered.Count > 0)
                {
                    resources.AddRange(discovered);
                }
            }

            resources = resources
                .Where(item => !string.IsNullOrWhiteSpace(item.ResourceName))
                .GroupBy(item => new
                {
                    item.ResourceType,
                    Name = item.ResourceName.Trim(),
                    Path = (item.ResourcePath ?? string.Empty).Trim()
                })
                .Select(group => group.First())
                .ToList();

            _logger.LogInformation(
                "Discovery toplandi. JobId={JobId}, TargetId={TargetId}, ResourceCount={ResourceCount}",
                context.JobId,
                context.TargetId,
                resources.Count);

            var now = DateTimeOffset.UtcNow;

            // Var olan kaynaklari temizleyip yeni kaynaklari yaz (0 sonuc olsa bile temizlemeliyiz).
            if (target.Resources.Count > 0)
            {
                _dbContext.JobResources.RemoveRange(target.Resources);
            }

            foreach (var resource in resources)
            {
                target.Resources.Add(new JobResource
                {
                    Id = Guid.NewGuid(),
                    JobTargetId = target.Id,
                    ResourceType = resource.ResourceType,
                    ResourceName = resource.ResourceName.Trim(),
                    ResourcePath = string.IsNullOrWhiteSpace(resource.ResourcePath)
                        ? string.Empty
                        : resource.ResourcePath.Trim(),
                    Status = TargetStatus.Success,
                    UpdatedAt = now
                });
            }

            target.Status = TargetStatus.Success;
            target.ErrorCode = null;
            target.ErrorMessage = null;
            target.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new DiscoveryResult
            {
                Status = TargetStatus.Success,
                Resources = resources
            };
        }
        catch (OperationFailureException ex)
        {
            _logger.LogWarning(
                "Discovery strategy hatasi. JobId={JobId}, TargetId={TargetId}, ErrorCode={ErrorCode}",
                context.JobId,
                context.TargetId,
                ex.ErrorCode);
            return await FailAndUpdate(target, ex.ErrorCode, ex.Message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discovery engine hatasi. JobId={JobId}, TargetId={TargetId}", context.JobId, context.TargetId);
            return await FailAndUpdate(target, "UNKNOWN", "Discovery engine hata verdi.", cancellationToken);
        }
    }

    private static DiscoveryResult Failure(string errorCode, string errorMessage)
    {
        return new DiscoveryResult
        {
            Status = TargetStatus.Failed,
            ErrorCode = errorCode,
            ErrorMessage = SensitiveDataRedactor.Redact(errorMessage)
        };
    }

    private async Task<DiscoveryResult> FailAndUpdate(
        JobTarget target,
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
