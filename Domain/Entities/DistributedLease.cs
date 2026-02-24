namespace Domain.Entities;

/// <summary>
/// Multi-instance calismada tekil calismasi gereken isler icin DB lease kaydi.
/// </summary>
public sealed class DistributedLease
{
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public DateTimeOffset LeaseUntilUtc { get; set; }
    public DateTimeOffset AcquiredAtUtc { get; set; }
    public DateTimeOffset RenewedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
