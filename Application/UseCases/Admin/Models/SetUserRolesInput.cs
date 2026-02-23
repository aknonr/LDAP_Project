namespace Application.UseCases.Admin.Models;

public sealed record SetUserRolesInput
{
    public Guid UserId { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

