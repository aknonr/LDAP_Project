namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Discovery job olusturma girdisi.
/// </summary>
public sealed record CreateDiscoveryJobInput
{
    public string ServerGroupExternalId { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public string? TicketRef { get; init; }
    public string? CorrelationId { get; init; }
}
