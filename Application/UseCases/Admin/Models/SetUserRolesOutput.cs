namespace Application.UseCases.Admin.Models;

public sealed record SetUserRolesOutput
{
    public Guid UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
