using Application.Abstractions.Directory;
using Application.Abstractions.Security;
using Application.Messaging;
using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

/// <summary>
/// Password-change job orkestrasyonu:
/// AD (LDAPS) change (old+new) -> update -> (opsiyonel) verify.
/// </summary>
public sealed class StartPasswordChangeJobConsumer : IConsumer<StartPasswordChangeJobCommand>
{
    private readonly ILogger<StartPasswordChangeJobConsumer> _logger;
    private readonly AdpmDbContext _dbContext;
    private readonly IPayloadProtector _payloadProtector;
    private readonly IAdPasswordChangeService _adPasswordChangeService;

    public StartPasswordChangeJobConsumer(
        ILogger<StartPasswordChangeJobConsumer> logger,
        AdpmDbContext dbContext,
        IPayloadProtector payloadProtector,
        IAdPasswordChangeService adPasswordChangeService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _payloadProtector = payloadProtector;
        _adPasswordChangeService = adPasswordChangeService;
    }

    public async Task Consume(ConsumeContext<StartPasswordChangeJobCommand> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Password-change orkestrasyonu basladi. JobId={JobId}",
            message.JobId);

        var job = await _dbContext.Jobs
            .Include(item => item.Targets)
            .FirstOrDefaultAsync(item => item.Id == message.JobId, context.CancellationToken);

        if (job is null)
        {
            _logger.LogWarning("Password-change job bulunamadi. JobId={JobId}", message.JobId);
            return;
        }

        if (job.Type != JobType.PasswordChange)
        {
            _logger.LogWarning(
                "Password-change orkestrator yanlis job type. JobId={JobId}, Type={Type}",
                job.Id,
                job.Type);
            return;
        }

        if (job.Targets.Count == 0)
        {
            _logger.LogWarning("Password-change job target yok. JobId={JobId}", job.Id);
            return;
        }

        // Job daha once tamamlandiysa tekrar calistirmayiz.
        if (job.Status is JobStatus.Success or JobStatus.Failed or JobStatus.Cancelled)
        {
            _logger.LogInformation(
                "Password-change orkestrasyonu atlandi (terminal). JobId={JobId}, Status={Status}",
                job.Id,
                job.Status);
            return;
        }

        await context.Publish(new JobProgressEvent
        {
            JobId = job.Id,
            Status = JobStatus.InProgress,
            Message = "AD password change basladi.",
            CorrelationId = message.CorrelationId
        }, context.CancellationToken);

        string oldPassword;
        string newPassword;
        try
        {
            oldPassword = await _payloadProtector.DecryptAsync(message.EncryptedOldPassword, context.CancellationToken);
            newPassword = await _payloadProtector.DecryptAsync(message.EncryptedNewPassword, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Password-change payload decrypt basarisiz. JobId={JobId}",
                job.Id);

            await FailAllTargetsAsync(
                context,
                job,
                "PAYLOAD_DECRYPT_FAILED",
                "Payload decrypt basarisiz.",
                message.CorrelationId);
            return;
        }

        var adChangeResult = await _adPasswordChangeService.ChangePasswordAsync(
            new AdPasswordChangeRequest
            {
                // TargetAccount (DOMAIN\\user | user@domain | DN) -> servis DN resolve eder.
                UserDnOrUpn = message.TargetAccount,
                OldPassword = oldPassword,
                NewPassword = newPassword
            },
            context.CancellationToken);

        if (!adChangeResult.IsSuccess)
        {
            var errorCode = adChangeResult.ErrorCode ?? "UNKNOWN";
            var errorMessage = adChangeResult.ErrorMessage ?? "AD password change basarisiz.";

            _logger.LogWarning(
                "AD password change basarisiz. JobId={JobId}, ErrorCode={ErrorCode}",
                job.Id,
                errorCode);

            await context.Publish(new JobProgressEvent
            {
                JobId = job.Id,
                Status = JobStatus.Failed,
                Message = $"AD password change basarisiz: {errorCode}",
                CorrelationId = message.CorrelationId
            }, context.CancellationToken);

            await FailAllTargetsAsync(context, job, errorCode, errorMessage, message.CorrelationId);
            return;
        }

        await context.Publish(new JobProgressEvent
        {
            JobId = job.Id,
            Status = JobStatus.InProgress,
            Message = "AD password change basarili. Update komutlari gonderiliyor.",
            CorrelationId = message.CorrelationId
        }, context.CancellationToken);

        // Update komutlarini server.update.commands queue'suna gonderir.
        var updateEndpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.ServerUpdateCommands}"));
        foreach (var target in job.Targets)
        {
            await updateEndpoint.Send(new UpdateServerResourcesCommand
            {
                JobId = job.Id,
                TargetId = target.Id,
                ServerName = target.ServerName,
                IpAddress = target.IpAddress,
                TargetAccount = message.TargetAccount,
                EncryptedPassword = message.EncryptedNewPassword,
                CorrelationId = message.CorrelationId
            }, context.CancellationToken);
        }

        await context.Publish(new JobProgressEvent
        {
            JobId = job.Id,
            Status = JobStatus.InProgress,
            Message = $"Update komutlari gonderildi. TargetCount={job.Targets.Count}",
            CorrelationId = message.CorrelationId
        }, context.CancellationToken);
    }

    private static async Task FailAllTargetsAsync(
        ConsumeContext<StartPasswordChangeJobCommand> context,
        Domain.Entities.Job job,
        string errorCode,
        string errorMessage,
        string? correlationId)
    {
        foreach (var target in job.Targets)
        {
            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = job.Id,
                TargetId = target.Id,
                Status = TargetStatus.Failed,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CorrelationId = correlationId
            }, context.CancellationToken);
        }
    }
}

