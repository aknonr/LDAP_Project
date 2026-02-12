using Application.Messaging;

namespace Application.Abstractions.Security;

/// <summary>
/// MQ payload sifreleme/de-sifreleme icin soyutlama.
/// </summary>
public interface IPayloadProtector
{
    /// <summary>
    /// Plain payload'i AES-GCM ile sifreler ve paketler.
    /// </summary>
    Task<EncryptedPayload> EncryptAsync(string plainText, CancellationToken cancellationToken);

    /// <summary>
    /// AES-GCM payload'i cozer ve plain text dondurur.
    /// </summary>
    Task<string> DecryptAsync(EncryptedPayload payload, CancellationToken cancellationToken);
}
