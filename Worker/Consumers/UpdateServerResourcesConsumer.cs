using Application.Abstractions.Discovery;
using Application.Abstractions.Security;
using Application.Abstractions.Update;
using Application.Models;
using Application.Messaging;
using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.Configuration;

namespace Worker.Consumers;

public sealed class UpdateServerResourcesConsumer : IConsumer<UpdateServerResourcesCommand>
{
    private static readonly ResourceType[] SupportedUpdateResourceTypes =
    [
        ResourceType.Service,
        ResourceType.ScheduledTask,
        ResourceType.IISAppPool,
        ResourceType.IISSite,
        ResourceType.IISWebApp,
        ResourceType.IISVirtualDir,
        ResourceType.COMPlus
    ];

    private readonly ILogger<UpdateServerResourcesConsumer> _logger;
    private readonly AdpmDbContext _dbContext;
    private readonly IReadOnlyCollection<IDiscoveryStrategy> _discoveryStrategies;
    private readonly IUpdateEngine _updateEngine;
    private readonly IPayloadProtector _payloadProtector;
    private readonly IOptions<VerificationOptions> _verificationOptions;

    public UpdateServerResourcesConsumer(
        ILogger<UpdateServerResourcesConsumer> logger,
        AdpmDbContext dbContext,
        IEnumerable<IDiscoveryStrategy> discoveryStrategies,
        IUpdateEngine updateEngine,
        IPayloadProtector payloadProtector,
        IOptions<VerificationOptions> verificationOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _discoveryStrategies = discoveryStrategies
            .Where(item => SupportedUpdateResourceTypes.Contains(item.ResourceType))
            .GroupBy(item => item.ResourceType)
            .Select(group => group.First())
            .ToList();
        _updateEngine = updateEngine;
        _payloadProtector = payloadProtector;
        _verificationOptions = verificationOptions;
    }

    public async Task Consume(ConsumeContext<UpdateServerResourcesCommand> context)
    {
        _logger.LogInformation(
            "Update command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
            context.Message.JobId,
            context.Message.TargetId,
            context.Message.ServerName);

        if (string.IsNullOrWhiteSpace(context.Message.TargetAccount))
        {
            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Failed,
                ErrorCode = "TARGET_ACCOUNT_REQUIRED",
                ErrorMessage = "Target account zorunlu.",
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);
            return;
        }

        int preparedCount;
        try
        {
            preparedCount = await EnsureResourcesPreparedAsync(context.Message, context.CancellationToken);
        }
        catch (OperationFailureException ex)
        {
            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Failed,
                ErrorCode = ex.ErrorCode,
                ErrorMessage = ex.Message,
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Update icin discovery hazirligi hata verdi. JobId={JobId}, TargetId={TargetId}",
                context.Message.JobId,
                context.Message.TargetId);

            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Failed,
                ErrorCode = "UNKNOWN",
                ErrorMessage = "Update icin discovery hazirligi hata verdi.",
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);
            return;
        }

        if (preparedCount == 0)
        {
            // Bu hedefte target account kullanan kaynak yoksa update no-op'tur.
            await MarkTargetNoOpSuccessAsync(context.Message.JobId, context.Message.TargetId, context.CancellationToken);

            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Success,
                ErrorCode = null,
                ErrorMessage = null,
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);

            await context.Publish(new JobProgressEvent
            {
                JobId = context.Message.JobId,
                Status = JobStatus.InProgress,
                Message = $"Update skip (no-op): target account kullanimi yok. Server={context.Message.ServerName}",
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);

            return;
        }

        string newPassword;
        try
        {
            newPassword = await _payloadProtector.DecryptAsync(context.Message.EncryptedPassword, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Payload decrypt basarisiz. JobId={JobId}, TargetId={TargetId}",
                context.Message.JobId,
                context.Message.TargetId);

            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Failed,
                ErrorCode = "PAYLOAD_DECRYPT_FAILED",
                ErrorMessage = "Payload decrypt basarisiz.",
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);
            return;
        }

        var result = await _updateEngine.UpdateAsync(
            new UpdateContext
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                ServerName = context.Message.ServerName,
                IpAddress = context.Message.IpAddress,
                TargetAccount = context.Message.TargetAccount,
                NewPassword = newPassword,
                CorrelationId = context.Message.CorrelationId
            },
            context.CancellationToken);

        if (result.Status == TargetStatus.Success && _verificationOptions.Value.EnablePostUpdateVerification)
        {
            var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.ServerVerifyCommands}"));
            await endpoint.Send(
                new VerifyServerCommand
                {
                    JobId = context.Message.JobId,
                    TargetId = context.Message.TargetId,
                    ServerName = context.Message.ServerName,
                    IpAddress = context.Message.IpAddress,
                    TargetAccount = context.Message.TargetAccount,
                    EncryptedPassword = context.Message.EncryptedPassword,
                    CorrelationId = context.Message.CorrelationId
                },
                sendContext =>
                {
                    // Ayni target verify komutu tekrar dispatch edilirse stabil MessageId kullan.
                    sendContext.MessageId = DeterministicMessageIdFactory.ForVerify(
                        context.Message.JobId,
                        context.Message.TargetId);
                },
                context.CancellationToken);

            // Verify sonuclanana kadar hedefi InProgress tutariz (final Success/Failed verify ile gelir).
            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.InProgress,
                ErrorCode = null,
                ErrorMessage = null,
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);

            await context.Publish(new JobProgressEvent
            {
                JobId = context.Message.JobId,
                Status = JobStatus.InProgress,
                Message = $"Target verify kuyruguna alindi: {context.Message.ServerName}",
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);

            return;
        }

        await context.Publish(new ServerUpdateResultEvent
        {
            JobId = context.Message.JobId,
            TargetId = context.Message.TargetId,
            Status = result.Status,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage,
            CorrelationId = context.Message.CorrelationId
        }, context.CancellationToken);
    }

    private async Task<int> EnsureResourcesPreparedAsync(UpdateServerResourcesCommand message, CancellationToken cancellationToken)
    {
        var target = await _dbContext.JobTargets
            .Include(item => item.Resources)
            .FirstOrDefaultAsync(item => item.JobId == message.JobId && item.Id == message.TargetId, cancellationToken);

        if (target is null)
        {
            return 0;
        }

        if (target.Resources.Count > 0)
        {
            // Kaynaklar onceden olusturulduysa, update engine mevcut kayitlar uzerinden devam eder.
            return target.Resources.Count;
        }

        var now = DateTimeOffset.UtcNow;
        var discoveryContext = new DiscoveryContext
        {
            JobId = message.JobId,
            TargetId = message.TargetId,
            ServerName = message.ServerName,
            IpAddress = message.IpAddress,
            CorrelationId = message.CorrelationId
        };

        var discovered = new List<DiscoveredResource>();
        foreach (var strategy in _discoveryStrategies)
        {
            var resources = await strategy.DiscoverAsync(discoveryContext, cancellationToken);
            if (resources.Count > 0)
            {
                discovered.AddRange(resources);
            }
        }

        var relevant = discovered
            .Where(item => !string.IsNullOrWhiteSpace(item.ResourceName))
            .Where(item => IsIdentityMatch(item.ResourceType, item.ResourcePath, message.TargetAccount))
            .GroupBy(item => new
            {
                item.ResourceType,
                Name = item.ResourceName.Trim(),
                Path = (item.ResourcePath ?? string.Empty).Trim()
            })
            .Select(group => group.First())
            .ToList();

        if (relevant.Count == 0)
        {
            return 0;
        }

        var existingKeys = target.Resources
            .Select(item => ResourceIdentityKey.From(item.ResourceType, item.ResourceName, item.ResourcePath))
            .ToHashSet();

        var addedCount = 0;
        foreach (var resource in relevant)
        {
            var key = ResourceIdentityKey.From(resource.ResourceType, resource.ResourceName, resource.ResourcePath);
            if (!existingKeys.Add(key))
            {
                continue;
            }

            target.Resources.Add(new Domain.Entities.JobResource
            {
                Id = Guid.NewGuid(),
                JobTargetId = target.Id,
                ResourceType = resource.ResourceType,
                ResourceName = resource.ResourceName.Trim(),
                ResourcePath = string.IsNullOrWhiteSpace(resource.ResourcePath)
                    ? string.Empty
                    : resource.ResourcePath.Trim(),
                Status = TargetStatus.Pending,
                UpdatedAt = now
            });
            addedCount++;
        }

        if (addedCount == 0)
        {
            return target.Resources.Count;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return target.Resources.Count;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(
                ex,
                "Resource hazirligi yarista tekrarlandi; mevcut kayitlar kullanilacak. JobId={JobId}, TargetId={TargetId}, Server={Server}",
                message.JobId,
                message.TargetId,
                message.ServerName);

            // Hata alan izde kalan Added entity'leri sonraki akislari etkilemesin.
            _dbContext.ChangeTracker.Clear();

            return await _dbContext.JobResources
                .AsNoTracking()
                .CountAsync(item => item.JobTargetId == message.TargetId, cancellationToken);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
        {
            return false;
        }

        return sqlException.Number is 2601 or 2627;
    }

    private readonly record struct ResourceIdentityKey(ResourceType ResourceType, string ResourceName, string ResourcePath)
    {
        public static ResourceIdentityKey From(ResourceType resourceType, string resourceName, string? resourcePath)
            => new(
                resourceType,
                resourceName.Trim(),
                (resourcePath ?? string.Empty).Trim());
    }

    private async Task MarkTargetNoOpSuccessAsync(Guid jobId, Guid targetId, CancellationToken cancellationToken)
    {
        var target = await _dbContext.JobTargets
            .FirstOrDefaultAsync(item => item.JobId == jobId && item.Id == targetId, cancellationToken);

        if (target is null)
        {
            return;
        }

        target.Status = TargetStatus.Success;
        target.ErrorCode = null;
        target.ErrorMessage = null;
        target.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsIdentityMatch(ResourceType resourceType, string? resourcePath, string targetAccount)
    {
        if (string.IsNullOrWhiteSpace(resourcePath) || string.IsNullOrWhiteSpace(targetAccount))
        {
            return false;
        }

        var identity = resourceType switch
        {
            ResourceType.IISSite or ResourceType.IISWebApp or ResourceType.IISVirtualDir => ExtractIisIdentity(resourcePath),
            _ => resourcePath
        };

        return !string.IsNullOrWhiteSpace(identity)
               && identity.Equals(targetAccount, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractIisIdentity(string resourcePath)
    {
        var separatorIndex = resourcePath.LastIndexOf('|');
        return separatorIndex >= 0
            ? resourcePath[(separatorIndex + 1)..]
            : resourcePath;
    }
}
