using API.Auth;
using Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

/// <summary>
/// Job bazli canli guncelleme aboneliklerini yoneten SignalR hub.
/// </summary>
[Authorize(Roles = "Admin,Operator,Viewer,SuperAdmin")]
public sealed class JobsHub : Hub
{
    public const string JobUpdatedEventName = "jobUpdated";
    public const string TargetUpdatedEventName = "targetUpdated";

    private readonly IJobRepository _jobRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<JobsHub> _logger;

    public JobsHub(
        IJobRepository jobRepository,
        IAuthorizationService authorizationService,
        ILogger<JobsHub> logger)
    {
        _jobRepository = jobRepository;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Istemciyi ilgili job grubuna ekler.
    /// </summary>
    public async Task SubscribeJob(Guid jobId)
    {
        var user = Context.User;
        if (user is null)
        {
            _logger.LogWarning("SignalR subscribe reddedildi (user yok). JobId={JobId}", jobId);
            Context.Abort();
            return;
        }

        var accessInfo = await _jobRepository.GetAccessInfoAsync(jobId, Context.ConnectionAborted);
        if (accessInfo is null)
        {
            _logger.LogWarning("SignalR subscribe reddedildi (job yok). JobId={JobId}", jobId);
            Context.Abort();
            return;
        }

        var authz = await _authorizationService.AuthorizeAsync(user, accessInfo, AuthorizationPolicies.JobAccess);
        if (!authz.Succeeded)
        {
            _logger.LogWarning("SignalR subscribe reddedildi (yetkisiz). JobId={JobId}", jobId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, BuildJobGroup(jobId));
    }

    /// <summary>
    /// Istemciyi ilgili job grubundan cikarir.
    /// </summary>
    public async Task UnsubscribeJob(Guid jobId)
    {
        var user = Context.User;
        if (user is null)
        {
            Context.Abort();
            return;
        }

        var accessInfo = await _jobRepository.GetAccessInfoAsync(jobId, Context.ConnectionAborted);
        if (accessInfo is null)
        {
            Context.Abort();
            return;
        }

        var authz = await _authorizationService.AuthorizeAsync(user, accessInfo, AuthorizationPolicies.JobAccess);
        if (!authz.Succeeded)
        {
            Context.Abort();
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildJobGroup(jobId));
    }

    /// <summary>
    /// Job odakli broadcast icin grup adini uretir.
    /// </summary>
    public static string BuildJobGroup(Guid jobId)
    {
        return $"job:{jobId:D}";
    }
}
