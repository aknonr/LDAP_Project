namespace API.Contracts.Auth;

/// <summary>
/// OIDC login baslatma istegi icin gerekli alanlari tasir.
/// </summary>
public sealed record LoginRequest
{
    public string? ReturnUrl { get; init; }
}
