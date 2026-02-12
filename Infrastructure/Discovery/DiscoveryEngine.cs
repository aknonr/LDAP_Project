using Application.Abstractions.Discovery;
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

            if (resources.Count == 0)
            {
                return await FailAndUpdate(target, "NOT_IMPLEMENTED", "Discovery engine henuz entegre edilmedi.", cancellationToken);
            }

            // Var olan kaynaklari temizleyip yeni kaynaklari yaz.
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
                    ResourceName = resource.ResourceName,
                    ResourcePath = resource.ResourcePath,
                    Status = TargetStatus.Success,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            target.Status = TargetStatus.Success;
            target.ErrorCode = null;
            target.ErrorMessage = null;
            target.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new DiscoveryResult
            {
                Status = TargetStatus.Success,
                Resources = resources
            };
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
