namespace Infrastructure.Security;

/// <summary>
/// AES-GCM payload sifreleme ayarlari.
/// </summary>
public sealed class PayloadEncryptionOptions
{
    public string Algorithm { get; set; } = "AES-GCM";
    public string KeyId { get; set; } = string.Empty;
    public string? SharedKeyBase64 { get; set; }
}
