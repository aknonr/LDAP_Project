namespace Infrastructure.Directory;

/// <summary>
/// LDAPS baglanti ayarlari.
/// </summary>
public sealed class LdapOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 636;
    public bool UseSsl { get; set; } = true;
    public bool ValidateServerCertificate { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 10;
    public string AuthType { get; set; } = "Negotiate";
}
