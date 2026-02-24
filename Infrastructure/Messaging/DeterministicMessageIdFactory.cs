using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Messaging;

/// <summary>
/// Ayni is mantigina ait tekrar publish durumlarinda stabil MessageId uretir.
/// Inbox dedupe (MessageId+ConsumerId) ile multi-instance tekrarlarini azaltir.
/// </summary>
public static class DeterministicMessageIdFactory
{
    public static Guid ForStartPasswordChangeJob(Guid jobId)
        => Create("start-password-change", jobId.ToString("D"));

    public static Guid ForDiscovery(Guid jobId, Guid targetId)
        => Create("discovery", jobId.ToString("D"), targetId.ToString("D"));

    public static Guid ForUpdate(Guid jobId, Guid targetId)
        => Create("update", jobId.ToString("D"), targetId.ToString("D"));

    public static Guid ForVerify(Guid jobId, Guid targetId)
        => Create("verify", jobId.ToString("D"), targetId.ToString("D"));

    private static Guid Create(params string[] parts)
    {
        var canonical = string.Join("|", parts.Select(item => item.Trim()));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        var bytes = hash.AsSpan(0, 16).ToArray();
        return new Guid(bytes);
    }
}
