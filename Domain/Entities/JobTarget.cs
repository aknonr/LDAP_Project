using Domain.Enums;

namespace Domain.Entities;

public sealed class JobTarget
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string ServerName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public TargetStatus Status { get; set; } = TargetStatus.Pending;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<JobResource> Resources { get; set; } = new List<JobResource>();
}
