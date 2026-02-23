using Infrastructure.Security;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Infrastructure.Tests;

public sealed class AesGcmPayloadProtectorTests
{
    [Fact]
    public async Task EncryptDecrypt_Roundtrip_Works()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var options = Options.Create(new PayloadEncryptionOptions
        {
            Algorithm = "AES-GCM",
            KeyId = "test-key",
            SharedKeyBase64 = Convert.ToBase64String(key)
        });

        var protector = new AesGcmPayloadProtector(options);

        var encrypted = await protector.EncryptAsync("hello", CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(encrypted.Ciphertext));
        Assert.False(string.IsNullOrWhiteSpace(encrypted.Nonce));
        Assert.False(string.IsNullOrWhiteSpace(encrypted.Tag));
        Assert.Equal("test-key", encrypted.KeyId);

        var plain = await protector.DecryptAsync(encrypted, CancellationToken.None);

        Assert.Equal("hello", plain);
    }

    [Fact]
    public async Task Decrypt_WithWrongKey_Throws()
    {
        var key1 = RandomNumberGenerator.GetBytes(32);
        var key2 = RandomNumberGenerator.GetBytes(32);

        var options1 = Options.Create(new PayloadEncryptionOptions
        {
            Algorithm = "AES-GCM",
            KeyId = "k1",
            SharedKeyBase64 = Convert.ToBase64String(key1)
        });

        var options2 = Options.Create(new PayloadEncryptionOptions
        {
            Algorithm = "AES-GCM",
            KeyId = "k2",
            SharedKeyBase64 = Convert.ToBase64String(key2)
        });

        var p1 = new AesGcmPayloadProtector(options1);
        var p2 = new AesGcmPayloadProtector(options2);

        var encrypted = await p1.EncryptAsync("secret", CancellationToken.None);

        await Assert.ThrowsAnyAsync<CryptographicException>(() => p2.DecryptAsync(encrypted, CancellationToken.None));
    }
}

