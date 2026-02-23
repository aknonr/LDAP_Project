namespace Application.UseCases.Admin.Models;

public sealed record SetUserActiveInput
{
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
}

