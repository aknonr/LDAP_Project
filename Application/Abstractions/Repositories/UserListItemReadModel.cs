namespace Application.Abstractions.Repositories;

/// <summary>
/// Kullanici listeleme icin read-model (EF entity dondurmeden).
/// </summary>
public sealed record UserListItemReadModel
{
    public Guid UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

