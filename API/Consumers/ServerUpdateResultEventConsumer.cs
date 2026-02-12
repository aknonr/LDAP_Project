using API.Contracts.Realtime;
using API.Hubs;
using Application.Abstractions.Tracking;
using Application.Messaging.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace API.Consumers;

/// <summary>
/// Password update result event'lerini DB + SignalR tarafina uygular.
/// </summary>
public sealed class ServerUpdateResultEventConsumer : IConsumer<ServerUpdateResultEvent>
{
    private readonly IJobTrackingService _jobTrackingService;
    private readonly IHubContext<JobsHub> _hubContext;
    private readonly ILogger<ServerUpdateResultEventConsumer> _logger;

    public ServerUpdateResultEventConsumer(
        IJobTrackingService jobTrackingService,
        IHubContext<JobsHub> hubContext,
        ILogger<ServerUpdateResultEventConsumer> logger)
    {
        _jobTrackingService = jobTrackingService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ServerUpdateResultEvent> context)
    {
        // Event geldikce hedef/job durumunu gunceller ve ilgili UI grubuna yayin yapar.
        var message = context.Message;
        var snapshot = await _jobTrackingService.ApplyTargetResultAsync(
            message.JobId,
            message.TargetId,
            message.Status,
            message.ErrorCode,
            message.ErrorMessage,
            message.CompletedAt,
            message.CorrelationId,
            context.CancellationToken);

        if (snapshot is null)
        {
            _logger.LogWarning(
                "ServerUpdateResultEvent atlandi. Job/Target bulunamadi. JobId={JobId}, TargetId={TargetId}",
                message.JobId,
                message.TargetId);
            return;
        }

        var groupName = JobsHub.BuildJobGroup(snapshot.JobId);
        await _hubContext.Clients.Group(groupName).SendAsync(
            JobsHub.TargetUpdatedEventName,
            new TargetUpdatedMessage
            {
                JobId = snapshot.JobId,
                TargetId = snapshot.TargetId,
                Status = snapshot.TargetStatus,
                ErrorCode = snapshot.ErrorCode,
                ErrorMessage = snapshot.ErrorMessage,
                UpdatedAt = snapshot.UpdatedAt,
                CorrelationId = snapshot.CorrelationId
            },
            context.CancellationToken);

        await _hubContext.Clients.Group(groupName).SendAsync(
            JobsHub.JobUpdatedEventName,
            new JobUpdatedMessage
            {
                JobId = snapshot.JobId,
                JobStatus = snapshot.JobStatus,
                UpdatedAt = snapshot.UpdatedAt,
                TargetCount = snapshot.TargetCount,
                SuccessCount = snapshot.SuccessCount,
                FailedCount = snapshot.FailedCount,
                CorrelationId = snapshot.CorrelationId
            },
            context.CancellationToken);
    }
}
