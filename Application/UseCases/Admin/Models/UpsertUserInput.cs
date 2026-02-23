namespace Application.UseCases.Admin.Models;

public sealed record UpsertUserInput
{
    public string Subject { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
}

