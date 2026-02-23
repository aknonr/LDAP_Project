namespace Application.UseCases.Admin.Models;

public sealed record ListUsersInput
{
    public int Skip { get; init; }
    public int Take { get; init; } = 100;
}

