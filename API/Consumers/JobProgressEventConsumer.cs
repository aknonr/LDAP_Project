using API.Contracts.Realtime;
using API.Hubs;
using Application.Abstractions.Tracking;
using Application.Messaging.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace API.Consumers;

/// <summary>
/// Job-level progress event'lerini DB + SignalR tarafina uygular.
/// </summary>
public sealed class JobProgressEventConsumer : IConsumer<JobProgressEvent>
{
    private readonly IJobTrackingService _jobTrackingService;
    private readonly IHubContext<JobsHub> _hubContext;
    private readonly ILogger<JobProgressEventConsumer> _logger;

    public JobProgressEventConsumer(
        IJobTrackingService jobTrackingService,
        IHubContext<JobsHub> hubContext,
        ILogger<JobProgressEventConsumer> logger)
    {
        _jobTrackingService = jobTrackingService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<JobProgressEvent> context)
    {
        // Job seviyesindeki event'i uygulayip ilgili istemcilere durum yansitir.
        var message = context.Message;
        var snapshot = await _jobTrackingService.ApplyJobProgressAsync(
            message.JobId,
            message.Status,
            message.Message,
            message.OccurredAt,
            message.CorrelationId,
            context.CancellationToken);

        if (snapshot is null)
        {
            _logger.LogWarning("JobProgressEvent atlandi. Job bulunamadi. JobId={JobId}", message.JobId);
            return;
        }

        await _hubContext.Clients.Group(JobsHub.BuildJobGroup(snapshot.JobId)).SendAsync(
            JobsHub.JobUpdatedEventName,
            new JobUpdatedMessage
            {
                JobId = snapshot.JobId,
                JobStatus = snapshot.JobStatus,
                Message = snapshot.Message,
                UpdatedAt = snapshot.UpdatedAt,
                TargetCount = snapshot.TargetCount,
                SuccessCount = snapshot.SuccessCount,
                FailedCount = snapshot.FailedCount,
                CorrelationId = snapshot.CorrelationId
            },
            context.CancellationToken);
    }
}
