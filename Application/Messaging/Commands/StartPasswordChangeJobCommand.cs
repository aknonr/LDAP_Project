using Application.Messaging;

namespace Application.Messaging.Commands;

/// <summary>
/// Password-change job orkestrasyonunu baslatan job-level komut (AD change -> update -> verify).
/// </summary>
public sealed record StartPasswordChangeJobCommand
{
    public Guid JobId { get; init; }
    public string TargetAccount { get; init; } = string.Empty;
    public EncryptedPayload EncryptedOldPassword { get; init; } = new();
    public EncryptedPayload EncryptedNewPassword { get; init; } = new();
    public string? CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

