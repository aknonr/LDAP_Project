using Application.Messaging.Commands;
using Application.Messaging.Events;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

public sealed class VerifyServerConsumer : IConsumer<VerifyServerCommand>
{
    private readonly ILogger<VerifyServerConsumer> _logger;

    public VerifyServerConsumer(ILogger<VerifyServerConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VerifyServerCommand> context)
    {
        // Verify isini baslatir ve stub sonuc publish eder (gercek verify akisi sonra eklenecek).
        _logger.LogInformation(
            "Verify command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
            context.Message.JobId,
            context.Message.TargetId,
            context.Message.ServerName);

        await context.Publish(new JobProgressEvent
        {
            JobId = context.Message.JobId,
            Status = JobStatus.Failed,
            Message = "Verify akisi henuz entegre edilmedi.",
            CorrelationId = context.Message.CorrelationId
        }, context.CancellationToken);
    }
}
