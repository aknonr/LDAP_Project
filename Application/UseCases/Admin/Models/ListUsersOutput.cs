namespace Application.UseCases.Admin.Models;

public sealed record ListUsersOutput
{
    public int TotalCount { get; init; }
    public IReadOnlyCollection<UserListItem> Items { get; init; } = Array.Empty<UserListItem>();
}

