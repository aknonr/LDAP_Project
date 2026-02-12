namespace API.Contracts.Audit;

/// <summary>
/// Audit listeleme endpoint response modeli.
/// </summary>
public sealed class AuditLogResponse
{
    public IReadOnlyList<AuditLogItem> Items { get; set; } = Array.Empty<AuditLogItem>();
}

/// <summary>
/// Tek audit kaydi.
/// </summary>
public sealed class AuditLogItem
{
    public Guid Id { get; set; }
    public string Who { get; set; } = string.Empty;
    public DateTimeOffset When { get; set; }
    public string? TicketRef { get; set; }
    public string TargetAccount { get; set; } = string.Empty;
    public string ServerGroup { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
