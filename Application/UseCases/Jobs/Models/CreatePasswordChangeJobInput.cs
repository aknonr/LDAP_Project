namespace Application.UseCases.Jobs.Models;

/// <summary>
/// Password change job olusturma girdisi (old+new).
/// </summary>
public sealed record CreatePasswordChangeJobInput
{
    public string ServerGroupExternalId { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public string RequestedBySubject { get; init; } = string.Empty;
    public string TargetAccount { get; init; } = string.Empty;
    public string OldPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string? TicketRef { get; init; }
    public string? CorrelationId { get; init; }
}
