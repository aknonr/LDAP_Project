using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.RemoteExecution;

public sealed partial class PowerShellWinRmCommandExecutor : IRemoteCommandExecutor
{
    private readonly IOptionsMonitor<RemoteExecutionOptions> _options;
    private readonly ILogger<PowerShellWinRmCommandExecutor> _logger;

    public PowerShellWinRmCommandExecutor(
        IOptionsMonitor<RemoteExecutionOptions> options,
        ILogger<PowerShellWinRmCommandExecutor> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<RemoteCommandExecutionResult> ExecutePowerShellAsync(
        string serverName,
        string scriptBlock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serverName) || !ServerNamePattern().IsMatch(serverName))
        {
            return RemoteCommandExecutionResult.Failure("WINRM_CONNECT_FAILED", "Gecersiz server adi.");
        }

        if (string.IsNullOrWhiteSpace(scriptBlock))
        {
            return RemoteCommandExecutionResult.Failure("UNKNOWN", "Script block bos olamaz.");
        }

        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return RemoteCommandExecutionResult.Failure("WINRM_CONNECT_FAILED", "Remote execution devre disi.");
        }

        if (!string.Equals(options.Transport, "WinRM.PowerShell", StringComparison.OrdinalIgnoreCase))
        {
            return RemoteCommandExecutionResult.Failure("WINRM_CONNECT_FAILED", "Desteklenmeyen remote transport.");
        }

        var command = BuildEncodedCommand(serverName, scriptBlock, options.ConnectTimeoutSeconds);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            var overallTimeout = Math.Max(1, options.OverallTimeoutSeconds);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(overallTimeout));

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(linkedCts.Token);

            var standardOutput = await outputTask;
            var standardError = await errorTask;

            if (process.ExitCode == 0)
            {
                return RemoteCommandExecutionResult.Success(standardOutput);
            }

            var mappedErrorCode = MapErrorCode(standardError);
            _logger.LogWarning(
                "Remote command basarisiz. Server={Server}, ExitCode={ExitCode}, ErrorCode={ErrorCode}",
                serverName,
                process.ExitCode,
                mappedErrorCode);

            return RemoteCommandExecutionResult.Failure(
                mappedErrorCode,
                "Remote command basarisiz.",
                standardError);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RemoteCommandExecutionResult.Failure("TIMEOUT", "Remote command timeout.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remote command exception. Server={Server}", serverName);
            return RemoteCommandExecutionResult.Failure("WINRM_CONNECT_FAILED", "Remote command calistirilamadi.");
        }
    }

    private static string BuildEncodedCommand(string serverName, string scriptBlock, int connectTimeoutSeconds)
    {
        var safeServerName = serverName.Replace("'", "''", StringComparison.Ordinal);
        var timeoutMilliseconds = Math.Max(1000, connectTimeoutSeconds * 1000);
        var script = $@"
$ErrorActionPreference = 'Stop'
Invoke-Command -ComputerName '{safeServerName}' -ScriptBlock {{
{scriptBlock}
}} -ErrorAction Stop -SessionOption (New-PSSessionOption -OperationTimeout {timeoutMilliseconds})
";

        return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }

    private static string MapErrorCode(string? standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return "UNKNOWN";
        }

        if (standardError.Contains("Access is denied", StringComparison.OrdinalIgnoreCase))
        {
            return "ACCESS_DENIED";
        }

        if (standardError.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return "TIMEOUT";
        }

        if (standardError.Contains("WinRM", StringComparison.OrdinalIgnoreCase)
            || standardError.Contains("PSSessionOpenFailed", StringComparison.OrdinalIgnoreCase)
            || standardError.Contains("cannot complete the operation", StringComparison.OrdinalIgnoreCase))
        {
            return "WINRM_CONNECT_FAILED";
        }

        return "UNKNOWN";
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9.-]{0,253}$")]
    private static partial Regex ServerNamePattern();
}
