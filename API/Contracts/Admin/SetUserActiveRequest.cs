namespace API.Contracts.Admin;

public sealed record SetUserActiveRequest
{
    public bool IsActive { get; init; }
    public string? TicketRef { get; init; }
}

