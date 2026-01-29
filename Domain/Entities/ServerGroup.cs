namespace Domain.Entities;

public sealed class ServerGroup
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<ServerInventory> Servers { get; set; } = new List<ServerInventory>();
}
