namespace API.Auth;

public sealed class OidcOptions
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? Audience { get; set; }
    public string GroupClaim { get; set; } = "groups";
    public List<string> AllowedGroups { get; set; } = new();
    public bool RequireHttpsMetadata { get; set; } = true;
}
