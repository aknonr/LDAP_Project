using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

public sealed class DiscoverServerUsageConsumer : IConsumer<DiscoverServerUsageCommand>
{
    private readonly ILogger<DiscoverServerUsageConsumer> _logger;

    public DiscoverServerUsageConsumer(ILogger<DiscoverServerUsageConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DiscoverServerUsageCommand> context)
    {
        // Discovery isini baslatir ve stub sonuc publish eder (gercek discovery engine sonra eklenecek).
        _logger.LogInformation(
            "Discovery command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
            context.Message.JobId,
            context.Message.TargetId,
            context.Message.ServerName);

        await context.Publish(new ServerUsageResultEvent
        {
            JobId = context.Message.JobId,
            TargetId = context.Message.TargetId,
            Status = TargetStatus.Failed,
            ErrorCode = "NOT_IMPLEMENTED",
            ErrorMessage = "Discovery engine henuz entegre edilmedi.",
            CorrelationId = context.Message.CorrelationId
        }, context.CancellationToken);
    }
}
