namespace Application.UseCases.Admin.Models;

public sealed record UpsertUserOutput
{
    public Guid UserId { get; init; }
    public bool IsCreated { get; init; }
}

