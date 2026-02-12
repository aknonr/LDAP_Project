using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Security;
using Application.Messaging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

public sealed class AesGcmPayloadProtector : IPayloadProtector
{
    private const int KeySizeInBytes = 32;
    private const int NonceSizeInBytes = 12;
    private const int TagSizeInBytes = 16;

    private readonly IOptions<PayloadEncryptionOptions> _options;

    public AesGcmPayloadProtector(IOptions<PayloadEncryptionOptions> options)
    {
        _options = options;
    }

    public Task<EncryptedPayload> EncryptAsync(string plainText, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new InvalidOperationException("Encrypt edilecek payload bos olamaz.");
        }

        var settings = _options.Value;
        if (!string.Equals(settings.Algorithm, "AES-GCM", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Desteklenmeyen payload sifreleme algoritmasi.");
        }

        if (string.IsNullOrWhiteSpace(settings.SharedKeyBase64))
        {
            throw new InvalidOperationException("Payload sifreleme anahtari tanimli degil.");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(settings.SharedKeyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Payload sifreleme anahtari Base64 formatinda olmali.", ex);
        }

        if (keyBytes.Length != KeySizeInBytes)
        {
            throw new InvalidOperationException("AES-GCM anahtar boyutu 256-bit (32 byte) olmalidir.");
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeInBytes];

        // Plain payload'i sadece bellekte sifreler; log/DB'de plain yazdirmaz.
        using (var aesGcm = new AesGcm(keyBytes, TagSizeInBytes))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        return Task.FromResult(new EncryptedPayload
        {
            Ciphertext = Convert.ToBase64String(cipherBytes),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            KeyId = settings.KeyId
        });
    }

    public Task<string> DecryptAsync(EncryptedPayload payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = _options.Value;
        if (!string.Equals(settings.Algorithm, "AES-GCM", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Desteklenmeyen payload sifreleme algoritmasi.");
        }

        if (string.IsNullOrWhiteSpace(settings.SharedKeyBase64))
        {
            throw new InvalidOperationException("Payload sifreleme anahtari tanimli degil.");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(settings.SharedKeyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Payload sifreleme anahtari Base64 formatinda olmali.", ex);
        }

        if (keyBytes.Length != KeySizeInBytes)
        {
            throw new InvalidOperationException("AES-GCM anahtar boyutu 256-bit (32 byte) olmalidir.");
        }

        if (string.IsNullOrWhiteSpace(payload.Ciphertext)
            || string.IsNullOrWhiteSpace(payload.Nonce)
            || string.IsNullOrWhiteSpace(payload.Tag))
        {
            throw new InvalidOperationException("Payload sifreleme alani eksik.");
        }

        var cipherBytes = Convert.FromBase64String(payload.Ciphertext);
        var nonce = Convert.FromBase64String(payload.Nonce);
        var tag = Convert.FromBase64String(payload.Tag);
        var plainBytes = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(keyBytes, TagSizeInBytes))
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Task.FromResult(Encoding.UTF8.GetString(plainBytes));
    }
}
