namespace Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Who { get; set; } = string.Empty;
    public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
    public string? TicketRef { get; set; }
    public string TargetAccount { get; set; } = string.Empty;
    public string ServerGroup { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
