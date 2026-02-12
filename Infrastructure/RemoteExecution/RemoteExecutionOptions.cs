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
}
