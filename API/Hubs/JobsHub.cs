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

    /// <summary>
    /// Istemciyi ilgili job grubuna ekler.
    /// </summary>
    public Task SubscribeJob(Guid jobId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, BuildJobGroup(jobId));
    }

    /// <summary>
    /// Istemciyi ilgili job grubundan cikarir.
    /// </summary>
    public Task UnsubscribeJob(Guid jobId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildJobGroup(jobId));
    }

    /// <summary>
    /// Job odakli broadcast icin grup adini uretir.
    /// </summary>
    public static string BuildJobGroup(Guid jobId)
    {
        return $"job:{jobId:D}";
    }
}
