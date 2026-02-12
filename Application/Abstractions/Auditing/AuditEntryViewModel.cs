namespace Application.Abstractions.Auditing;

/// <summary>
/// API tarafinda gosterilecek audit kaydi modeli.
/// </summary>
public sealed record AuditEntryViewModel
{
    public Guid Id { get; init; }
    public string Who { get; init; } = string.Empty;
    public DateTimeOffset When { get; init; }
    public string? TicketRef { get; init; }
    public string TargetAccount { get; init; } = string.Empty;
    public string ServerGroup { get; init; } = string.Empty;
    public string ResultSummary { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}
