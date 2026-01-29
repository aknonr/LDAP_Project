using Domain.Enums;

namespace Domain.Entities;

public sealed class JobResource
{
    public Guid Id { get; set; }
    public Guid JobTargetId { get; set; }
    public JobTarget JobTarget { get; set; } = null!;
    public ResourceType ResourceType { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string? ResourcePath { get; set; }
    public TargetStatus Status { get; set; } = TargetStatus.Pending;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
