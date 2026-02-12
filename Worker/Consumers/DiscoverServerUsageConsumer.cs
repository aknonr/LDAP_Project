using Application.Messaging.Commands;
using Application.Messaging.Events;
using Application.Abstractions.Discovery;
using Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Worker.Consumers;

public sealed class DiscoverServerUsageConsumer : IConsumer<DiscoverServerUsageCommand>
{
    private readonly ILogger<DiscoverServerUsageConsumer> _logger;
    private readonly IDiscoveryEngine _discoveryEngine;

    public DiscoverServerUsageConsumer(
        ILogger<DiscoverServerUsageConsumer> logger,
        IDiscoveryEngine discoveryEngine)
    {
        _logger = logger;
        _discoveryEngine = discoveryEngine;
    }

    public async Task Consume(ConsumeContext<DiscoverServerUsageCommand> context)
    {
        // Discovery isini calistirip sonuc event'ini publish eder.
        _logger.LogInformation(
            "Discovery command alindi. JobId={JobId}, TargetId={TargetId}, Server={Server}",
            context.Message.JobId,
            context.Message.TargetId,
            context.Message.ServerName);

        var result = await _discoveryEngine.DiscoverAsync(
            new DiscoveryContext
            {
                JobId = context.Message.JobId,
                TargetId = context.Message.TargetId,
                ServerName = context.Message.ServerName,
                IpAddress = context.Message.IpAddress,
                CorrelationId = context.Message.CorrelationId
            },
            context.CancellationToken);

        await context.Publish(new ServerUsageResultEvent
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
