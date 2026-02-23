namespace API.Auth;

public sealed record BootstrapOptions
{
    public bool Enabled { get; init; }
    public string? InitialAdminSubject { get; init; }
    public string? InitialAdminDisplayName { get; init; }
    public string? InitialAdminEmail { get; init; }
}

