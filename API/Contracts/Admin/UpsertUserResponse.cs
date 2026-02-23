namespace API.Contracts.Admin;

public sealed record UpsertUserResponse
{
    public Guid UserId { get; init; }
    public bool IsCreated { get; init; }
}

