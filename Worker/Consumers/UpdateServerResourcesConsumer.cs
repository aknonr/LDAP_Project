using Application.Messaging.Commands;
using Application.Messaging.Events;
using Application.Abstractions.Update;
using Application.Abstractions.Security;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

public sealed class UpdateServerResourcesConsumer : IConsumer<UpdateServerResourcesCommand>
{
    private readonly ILogger<UpdateServerResourcesConsumer> _logger;
    private readonly IUpdateEngine _updateEngine;
    private readonly IPayloadProtector _payloadProtector;

    public UpdateServerResourcesConsumer(
        ILogger<UpdateServerResourcesConsumer> logger,
        IUpdateEngine updateEngine,
        IPayloadProtector payloadProtector)
    {
        _logger = logger;
        _updateEngine = updateEngine;
        _payloadProtector = payloadProtector;
    }

    public async Task Consume(ConsumeContext<UpdateServerResourcesCommand> context)
    {
        // Update isini calistirir ve sonuc event'i publish eder.
        _logger.LogInformation(
            "Update command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
            context.Message.JobId,
            context.Message.TargetId,
            context.Message.ServerName);

        string newPassword;
        try
        {
            newPassword = await _payloadProtector.DecryptAsync(context.Message.EncryptedPassword, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payload decrypt basarisiz. JobId={JobId}, TargetId={TargetId}", context.Message.JobId, context.Message.TargetId);
            await context.Publish(new ServerUpdateResultEvent
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                Status = TargetStatus.Failed,
                ErrorCode = "UNKNOWN",
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
