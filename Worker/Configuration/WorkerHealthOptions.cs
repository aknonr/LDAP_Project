namespace Worker.Configuration;

/// <summary>
/// Worker bagimlilik sagligi ve sistem heartbeat ayarlari.
/// </summary>
public sealed class WorkerHealthOptions
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    public bool CheckDatabase { get; set; } = true;
}
