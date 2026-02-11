namespace API.Contracts.Jobs;

/// <summary>
/// Yeni job olustugunda donen temel bilgileri tasir.
/// </summary>
public sealed record JobCreatedResponse
{
    public Guid JobId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
