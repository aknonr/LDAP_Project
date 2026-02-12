using Application.Abstractions.Discovery;
using Application.Abstractions.Verify;
using Application.Models;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Verify;

/// <summary>
/// Update sonrasi, hedefteki kaynaklari tekrar discovery ederek identity eslesmesini dogrular.
/// </summary>
public sealed class VerifyEngine : IVerifyEngine
{
    private static readonly ResourceType[] SupportedResourceTypes =
    [
        ResourceType.Service,
        ResourceType.ScheduledTask,
        ResourceType.IISAppPool,
        ResourceType.IISSite,
        ResourceType.IISWebApp,
        ResourceType.IISVirtualDir,
        ResourceType.COMPlus
    ];

    private readonly AdpmDbContext _dbContext;
    private readonly IReadOnlyDictionary<ResourceType, IDiscoveryStrategy> _discoveryStrategies;
    private readonly ILogger<VerifyEngine> _logger;

    public VerifyEngine(
        AdpmDbContext dbContext,
        IEnumerable<IDiscoveryStrategy> discoveryStrategies,
        ILogger<VerifyEngine> logger)
    {
        _dbContext = dbContext;
        _discoveryStrategies = discoveryStrategies
            .GroupBy(item => item.ResourceType)
            .ToDictionary(group => group.Key, group => group.First());
        _logger = logger;
    }

    public async Task<VerifyResult> VerifyAsync(VerifyContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.TargetAccount))
        {
            return Failure("VERIFY_TARGET_ACCOUNT_REQUIRED", "Verify icin target account zorunlu.");
        }

        var target = await _dbContext.JobTargets
            .Include(item => item.Resources)
            .FirstOrDefaultAsync(item => item.JobId == context.JobId && item.Id == context.TargetId, cancellationToken);

        if (target is null)
        {
            return Failure("RESOURCE_NOT_FOUND", "Job target bulunamadi.");
        }

        if (target.Resources.Count == 0)
        {
            return await FailAndPersistTargetAsync(
                target,
                "RESOURCE_NOT_FOUND",
                "Verify icin target kaynagi bulunamadi.",
                cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        string? targetErrorCode = null;
        string? targetErrorMessage = null;

        var resourcesByType = target.Resources
            .Where(item => SupportedResourceTypes.Contains(item.ResourceType))
            .GroupBy(item => item.ResourceType);

        if (!resourcesByType.Any())
        {
            return await FailAndPersistTargetAsync(
                target,
                "VERIFY_NOT_SUPPORTED",
                "Verify desteklenen kaynak tipi bulunamadi.",
                cancellationToken);
        }

        foreach (var group in resourcesByType)
        {
            if (!_discoveryStrategies.TryGetValue(group.Key, out var strategy))
            {
                foreach (var resource in group)
                {
                    MarkResourceFailure(
                        resource,
                        "RESOURCE_NOT_FOUND",
                        "Verify discovery strategy bulunamadi.",
                        now);
                }

                targetErrorCode ??= "RESOURCE_NOT_FOUND";
                targetErrorMessage ??= "Verify discovery strategy bulunamadi.";
                continue;
            }

            IReadOnlyDictionary<string, string?> discoveredLookup;
            try
            {
                // Verify, mevcut kaynak kimliklerini dogrudan sunucudan tekrar okuyarak yapilir.
                var discoveredResources = await strategy.DiscoverAsync(
                    new DiscoveryContext
                    {
                        JobId = context.JobId,
                        TargetId = context.TargetId,
                        ServerName = context.ServerName,
                        IpAddress = context.IpAddress,
                        CorrelationId = context.CorrelationId
                    },
                    cancellationToken);

                discoveredLookup = discoveredResources
                    .GroupBy(item => item.ResourceName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        groupItem => groupItem.Key,
                        groupItem => groupItem.First().ResourcePath,
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationFailureException ex)
            {
                foreach (var resource in group)
                {
                    MarkResourceFailure(resource, ex.ErrorCode, ex.Message, now);
                }

                targetErrorCode ??= ex.ErrorCode;
                targetErrorMessage ??= ex.Message;
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Verify discovery hatasi. JobId={JobId}, TargetId={TargetId}, ResourceType={ResourceType}",
                    context.JobId,
                    context.TargetId,
                    group.Key);

                foreach (var resource in group)
                {
                    MarkResourceFailure(resource, "UNKNOWN", "Verify discovery hatasi.", now);
                }

                targetErrorCode ??= "UNKNOWN";
                targetErrorMessage ??= "Verify discovery hatasi.";
                continue;
            }

            foreach (var resource in group)
            {
                if (!discoveredLookup.TryGetValue(resource.ResourceName, out var discoveredPath))
                {
                    MarkResourceFailure(resource, "RESOURCE_NOT_FOUND", "Kaynak verify asamasinda bulunamadi.", now);
                    targetErrorCode ??= resource.ErrorCode;
                    targetErrorMessage ??= resource.ErrorMessage;
                    continue;
                }

                if (!IsIdentityMatch(resource.ResourceType, discoveredPath, context.TargetAccount))
                {
                    MarkResourceFailure(resource, "VERIFY_MISMATCH", "Kaynak hesabi hedef hesapla eslesmiyor.", now);
                    targetErrorCode ??= resource.ErrorCode;
                    targetErrorMessage ??= resource.ErrorMessage;
                    continue;
                }

                resource.Status = TargetStatus.Success;
                resource.ErrorCode = null;
                resource.ErrorMessage = null;
                resource.UpdatedAt = now;
            }
        }

        target.Status = target.Resources.Any(item => item.Status == TargetStatus.Failed)
            ? TargetStatus.Failed
            : TargetStatus.Success;
        target.ErrorCode = targetErrorCode;
        target.ErrorMessage = SensitiveDataRedactor.Redact(targetErrorMessage);
        target.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VerifyResult
        {
            Status = target.Status,
            ErrorCode = target.ErrorCode,
            ErrorMessage = target.ErrorMessage
        };
    }

    private static bool IsIdentityMatch(ResourceType resourceType, string? resourcePath, string targetAccount)
    {
        var identity = resourceType switch
        {
            ResourceType.IISSite or ResourceType.IISWebApp or ResourceType.IISVirtualDir => ExtractIisIdentity(resourcePath),
            _ => resourcePath
        };

        return !string.IsNullOrWhiteSpace(identity)
            && identity.Equals(targetAccount, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractIisIdentity(string? resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        var separatorIndex = resourcePath.LastIndexOf('|');
        return separatorIndex >= 0
            ? resourcePath[(separatorIndex + 1)..]
            : resourcePath;
    }

    private static void MarkResourceFailure(
        Domain.Entities.JobResource resource,
        string errorCode,
        string errorMessage,
        DateTimeOffset updatedAt)
    {
        resource.Status = TargetStatus.Failed;
        resource.ErrorCode = errorCode;
        resource.ErrorMessage = SensitiveDataRedactor.Redact(errorMessage);
        resource.UpdatedAt = updatedAt;
    }

    private static VerifyResult Failure(string errorCode, string errorMessage)
    {
        return new VerifyResult
        {
            Status = TargetStatus.Failed,
            ErrorCode = errorCode,
            ErrorMessage = SensitiveDataRedactor.Redact(errorMessage)
        };
    }

    private async Task<VerifyResult> FailAndPersistTargetAsync(
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
