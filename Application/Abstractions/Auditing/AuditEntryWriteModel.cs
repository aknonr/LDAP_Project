namespace Application.Abstractions.Auditing;

/// <summary>
/// Audit tablosuna yazilacak alanlar.
/// </summary>
public sealed record AuditEntryWriteModel
{
    public string Who { get; init; } = string.Empty;
    public DateTimeOffset When { get; init; } = DateTimeOffset.UtcNow;
    public string? TicketRef { get; init; }
    public string TargetAccount { get; init; } = string.Empty;
    public string ServerGroup { get; init; } = string.Empty;
    public string ResultSummary { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}
