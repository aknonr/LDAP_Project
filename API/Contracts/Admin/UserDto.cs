namespace API.Contracts.Admin;

public sealed record UserDto
{
    public Guid UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}

