namespace Infrastructure.RemoteExecution;

/// <summary>
/// Uzaktaki Windows sunucularda PowerShell calistirma sozlesmesi.
/// </summary>
public interface IRemoteCommandExecutor
{
    /// <summary>
    /// Sunucuda script block calistirir.
    /// </summary>
    Task<RemoteCommandExecutionResult> ExecutePowerShellAsync(
        string serverName,
        string scriptBlock,
        CancellationToken cancellationToken);
}
