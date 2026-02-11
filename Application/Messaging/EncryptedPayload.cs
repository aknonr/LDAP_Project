namespace Application.Messaging;

/// <summary>
/// AES-GCM ile sifrelenmis payload tasir (plain sifre asla gonderilmez).
/// </summary>
public sealed record EncryptedPayload
{
    public string Ciphertext { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
}
