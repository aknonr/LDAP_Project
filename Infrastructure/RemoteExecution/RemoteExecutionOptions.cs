namespace Infrastructure.RemoteExecution;

/// <summary>
/// WinRM/PowerShell uzaktan komut calistirma ayarlari.
/// </summary>
public sealed class RemoteExecutionOptions
{
    public bool Enabled { get; set; } = true;
    public string Transport { get; set; } = "WinRM.PowerShell";
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public int OverallTimeoutSeconds { get; set; } = 60;

    public RemoteCircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

public sealed class RemoteCircuitBreakerOptions
{
    public bool Enabled { get; set; } = true;
    public int TrackingPeriodSeconds { get; set; } = 60;
    public int MinimumThroughput { get; set; } = 20;
    public double FailureThreshold { get; set; } = 0.5;
    public int BreakDurationSeconds { get; set; } = 60;
}
