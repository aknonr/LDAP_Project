using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

public sealed class UpdateServerResourcesConsumer : IConsumer<UpdateServerResourcesCommand>
{
    private readonly ILogger<UpdateServerResourcesConsumer> _logger;

    public UpdateServerResourcesConsumer(ILogger<UpdateServerResourcesConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateServerResourcesCommand> context)
    {
        // Update isini baslatir ve stub sonuc publish eder (gercek update engine sonra ekleceðim).
        _logger.LogInformation(
            "Update command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
            context.Message.JobId,
            context.Message.TargetId,
            context.Message.ServerName);

        await context.Publish(new ServerUpdateResultEvent
        {
            JobId = context.Message.JobId,
            TargetId = context.Message.TargetId,
            Status = TargetStatus.Failed,
            ErrorCode = "NOT_IMPLEMENTED",
            ErrorMessage = "Update engine henuz entegre edilmedi.",
            CorrelationId = context.Message.CorrelationId
        }, context.CancellationToken);
    }
}
