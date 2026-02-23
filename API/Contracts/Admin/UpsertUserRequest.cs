namespace API.Contracts.Admin;

public sealed record UpsertUserRequest
{
    public string Subject { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? TicketRef { get; init; }
}

