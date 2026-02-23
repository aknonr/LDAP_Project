namespace Application.UseCases.Admin.Models;

public sealed record ListRolesOutput
{
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

