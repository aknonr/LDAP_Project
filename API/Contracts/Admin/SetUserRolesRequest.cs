namespace API.Contracts.Admin;

public sealed record SetUserRolesRequest
{
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public string? TicketRef { get; init; }
}

