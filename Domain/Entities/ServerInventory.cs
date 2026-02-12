namespace Domain.Entities;

public sealed class ServerInventory
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public Guid? ServerGroupId { get; set; }
    public ServerGroup? ServerGroup { get; set; }
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedDate { get; set; }
}
