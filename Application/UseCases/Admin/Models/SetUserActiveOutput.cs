namespace Application.UseCases.Admin.Models;

public sealed record SetUserActiveOutput
{
    public Guid UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
