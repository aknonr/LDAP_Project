namespace API.Contracts.Admin;

public sealed record RoleListResponse
{
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

