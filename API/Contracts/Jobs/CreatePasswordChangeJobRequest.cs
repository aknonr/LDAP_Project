namespace API.Contracts.Jobs;

/// <summary>
/// Password change job baslatmak icin kullanilir (old+new).
/// </summary>
public sealed record CreatePasswordChangeJobRequest
{
    public string ServerGroupId { get; init; } = string.Empty;
    public string TargetAccount { get; init; } = string.Empty;
    public string OldPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string? TicketRef { get; init; }
}
