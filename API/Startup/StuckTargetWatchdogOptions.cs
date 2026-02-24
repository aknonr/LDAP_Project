namespace API.Startup;

/// <summary>
/// Uzun sure InProgress kalan hedefler icin gozlem/iyilestirme ayarlari.
/// </summary>
public sealed class StuckTargetWatchdogOptions
{
    public bool Enabled { get; set; } = true;
    public int ScanIntervalSeconds { get; set; } = 60;
    public int TargetInProgressTimeoutSeconds { get; set; } = 1800;
    public int BatchSize { get; set; } = 200;
    public bool AutoFailTimedOutTargets { get; set; } = false;

    public bool UseDistributedLease { get; set; } = true;
    public string LeaseName { get; set; } = "stuck-target-watchdog";
    public int LeaseDurationSeconds { get; set; } = 120;
}
