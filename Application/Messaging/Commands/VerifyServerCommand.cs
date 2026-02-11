using Application.Messaging;

namespace Application.Messaging.Commands;

/// <summary>
/// Sifre degisikliginin dogrulanmasi icin gonderilen komut.
/// </summary>
public sealed record VerifyServerCommand
{
    public Guid JobId { get; init; }
    public Guid TargetId { get; init; }
    public string ServerName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public EncryptedPayload EncryptedPassword { get; init; } = new();
    public string? CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
