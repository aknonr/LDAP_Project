using Application.Abstractions.Security;
using Application.Abstractions.Verify;
using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

public sealed class VerifyServerConsumer : IConsumer<VerifyServerCommand>
{
    private readonly ILogger<VerifyServerConsumer> _logger;
    private readonly IVerifyEngine _verifyEngine;
    private readonly IPayloadProtector _payloadProtector;

    public VerifyServerConsumer(
        ILogger<VerifyServerConsumer> logger,
        IVerifyEngine verifyEngine,
        IPayloadProtector payloadProtector)
    {
        _logger = logger;
        _verifyEngine = verifyEngine;
        _payloadProtector = payloadProtector;
    }

    public async Task Consume(ConsumeContext<VerifyServerCommand> context)
    {
        _logger.LogInformation(
            "Verify command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
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
                ErrorCode = "VERIFY_TARGET_ACCOUNT_REQUIRED",
                ErrorMessage = "Verify icin target account zorunlu.",
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
                "Verify payload decrypt basarisiz. JobId={JobId}, TargetId={TargetId}",
                context.Message.JobId,
                context.Message.TargetId);

            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Failed,
                ErrorCode = "VERIFY_PAYLOAD_DECRYPT_FAILED",
                ErrorMessage = "Verify payload decrypt basarisiz.",
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);
            return;
        }

        var result = await _verifyEngine.VerifyAsync(
            new VerifyContext
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
}

