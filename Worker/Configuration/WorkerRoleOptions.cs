namespace Worker.Configuration;

/// <summary>
/// Worker instance'inin tukecegi kuyruk rollerini belirler.
/// </summary>
public sealed class WorkerRoleOptions
{
    public bool EnableDiscovery { get; set; } = true;
    public bool EnableUpdate { get; set; } = true;
    public bool EnableVerify { get; set; } = true;
    public bool EnableInventorySync { get; set; } = true;
}
