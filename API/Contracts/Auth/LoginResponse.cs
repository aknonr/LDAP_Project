namespace API.Contracts.Auth;

/// <summary>
/// OIDC login icin yonlendirme adresini dondurur.
/// </summary>
public sealed record LoginResponse
{
    public string LoginUrl { get; init; } = string.Empty;
}
