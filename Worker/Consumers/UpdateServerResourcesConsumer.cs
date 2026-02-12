using Application.Abstractions.Security;
using Application.Abstractions.Update;
using Application.Messaging;
using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Worker.Configuration;

namespace Worker.Consumers;

public sealed class UpdateServerResourcesConsumer : IConsumer<UpdateServerResourcesCommand>
{
    private readonly ILogger<UpdateServerResourcesConsumer> _logger;
    private readonly IUpdateEngine _updateEngine;
    private readonly IPayloadProtector _payloadProtector;
    private readonly IOptions<VerificationOptions> _verificationOptions;

    public UpdateServerResourcesConsumer(
        ILogger<UpdateServerResourcesConsumer> logger,
        IUpdateEngine updateEngine,
        IPayloadProtector payloadProtector,
        IOptions<VerificationOptions> verificationOptions)
    {
        _logger = logger;
        _updateEngine = updateEngine;
        _payloadProtector = payloadProtector;
        _verificationOptions = verificationOptions;
    }

    public async Task Consume(ConsumeContext<UpdateServerResourcesCommand> context)
    {
        // Update isini calistirir, gerekirse verify kuyruguna zincirler.
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

        if (result.Status == TargetStatus.Success && _verificationOptions.Value.EnablePostUpdateVerification)
        {
            // EndpointConvention burada garanti degil; explicit queue adresine gonderiyoruz.
            var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.ServerVerifyCommands}"));
            await endpoint.Send(new VerifyServerCommand
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                ServerName = context.Message.ServerName,
                IpAddress = context.Message.IpAddress,
                TargetAccount = context.Message.TargetAccount,
                EncryptedPassword = context.Message.EncryptedPassword,
                CorrelationId = context.Message.CorrelationId
            }, context.CancellationToken);

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
}
