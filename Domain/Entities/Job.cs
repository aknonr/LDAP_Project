using Domain.Enums;

namespace Domain.Entities;

public sealed class Job
{
    public Guid Id { get; set; }
    public JobType Type { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string RequestedBy { get; set; } = string.Empty;
    public Guid? ServerGroupId { get; set; }
    public ServerGroup? ServerGroup { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<JobTarget> Targets { get; set; } = new List<JobTarget>();
}
