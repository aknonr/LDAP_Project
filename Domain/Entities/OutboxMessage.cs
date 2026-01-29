namespace Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public DateTimeOffset? ProcessedOn { get; set; }
}
