namespace API.Contracts.Admin;

public sealed record ListUsersResponse
{
    public int TotalCount { get; init; }
    public IReadOnlyCollection<UserDto> Items { get; init; } = Array.Empty<UserDto>();
}

