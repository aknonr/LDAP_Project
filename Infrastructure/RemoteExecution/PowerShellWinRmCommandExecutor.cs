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

    private readonly object _circuitLock = new();
    private DateTimeOffset _circuitOpenUntil = DateTimeOffset.MinValue;
    private readonly Queue<(DateTimeOffset Timestamp, bool IsFailure)> _circuitWindow = new();
    private int _circuitFailureCount;

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

        if (options.CircuitBreaker.Enabled && IsCircuitOpen())
        {
            return RemoteCommandExecutionResult.Failure(
                "CIRCUIT_OPEN",
                "Remote execution circuit open (gecici olarak durduruldu).");
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
                TrackCircuitOutcome(options.CircuitBreaker, isFailure: false);
                return RemoteCommandExecutionResult.Success(standardOutput);
            }

            var mappedErrorCode = MapErrorCode(standardError);
            var summarizedError = SummarizeStandardError(standardError);
            var connectivityHint = BuildConnectivityHint(mappedErrorCode, standardError);
            TrackCircuitOutcome(options.CircuitBreaker, isFailure: IsSystemicFailure(mappedErrorCode));
            _logger.LogWarning(
                "Remote command basarisiz. Server={Server}, ExitCode={ExitCode}, ErrorCode={ErrorCode}, Transport={Transport}, ConnectTimeoutSeconds={ConnectTimeoutSeconds}, OverallTimeoutSeconds={OverallTimeoutSeconds}, Hint={Hint}, ErrorSummary={ErrorSummary}",
                serverName,
                process.ExitCode,
                mappedErrorCode,
                options.Transport,
                options.ConnectTimeoutSeconds,
                options.OverallTimeoutSeconds,
                connectivityHint,
                summarizedError);

            return RemoteCommandExecutionResult.Failure(
                mappedErrorCode,
                "Remote command basarisiz.",
                standardError);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TrackCircuitOutcome(options.CircuitBreaker, isFailure: true);
            _logger.LogWarning(
                "Remote command timeout. Server={Server}, Transport={Transport}, ConnectTimeoutSeconds={ConnectTimeoutSeconds}, OverallTimeoutSeconds={OverallTimeoutSeconds}, Hint={Hint}",
                serverName,
                options.Transport,
                options.ConnectTimeoutSeconds,
                options.OverallTimeoutSeconds,
                "WINRM timeout/firewall/network kaynakli olabilir.");
            return RemoteCommandExecutionResult.Failure("TIMEOUT", "Remote command timeout.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Remote command exception. Server={Server}, Transport={Transport}, ConnectTimeoutSeconds={ConnectTimeoutSeconds}, OverallTimeoutSeconds={OverallTimeoutSeconds}",
                serverName,
                options.Transport,
                options.ConnectTimeoutSeconds,
                options.OverallTimeoutSeconds);
            TrackCircuitOutcome(options.CircuitBreaker, isFailure: true);
            return RemoteCommandExecutionResult.Failure("WINRM_CONNECT_FAILED", "Remote command calistirilamadi.");
        }
    }

    private bool IsCircuitOpen()
    {
        var now = DateTimeOffset.UtcNow;
        lock (_circuitLock)
        {
            return now < _circuitOpenUntil;
        }
    }

    private void TrackCircuitOutcome(RemoteCircuitBreakerOptions options, bool isFailure)
    {
        if (!options.Enabled)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var trackingSeconds = Math.Max(10, options.TrackingPeriodSeconds);
        var windowStart = now - TimeSpan.FromSeconds(trackingSeconds);

        lock (_circuitLock)
        {
            // Window temizle
            while (_circuitWindow.Count > 0 && _circuitWindow.Peek().Timestamp < windowStart)
            {
                var removed = _circuitWindow.Dequeue();
                if (removed.IsFailure)
                {
                    _circuitFailureCount = Math.Max(0, _circuitFailureCount - 1);
                }
            }

            _circuitWindow.Enqueue((now, isFailure));
            if (isFailure)
            {
                _circuitFailureCount++;
            }

            var total = _circuitWindow.Count;
            var minThroughput = Math.Max(1, options.MinimumThroughput);
            if (total < minThroughput)
            {
                return;
            }

            var failureThreshold = options.FailureThreshold <= 0 ? 0.5 : options.FailureThreshold;
            var failureRatio = (double)_circuitFailureCount / total;
            if (failureRatio < failureThreshold)
            {
                return;
            }

            var breakSeconds = Math.Max(10, options.BreakDurationSeconds);
            _circuitOpenUntil = now.AddSeconds(breakSeconds);
            _circuitWindow.Clear();
            _circuitFailureCount = 0;

            _logger.LogWarning(
                "Remote execution circuit acildi. TrackingSeconds={TrackingSeconds}, FailureRatio={FailureRatio:0.00}, BreakSeconds={BreakSeconds}",
                trackingSeconds,
                failureRatio,
                breakSeconds);
        }
    }

    private static bool IsSystemicFailure(string errorCode)
    {
        return string.Equals(errorCode, "WINRM_CONNECT_FAILED", StringComparison.OrdinalIgnoreCase)
               || string.Equals(errorCode, "TIMEOUT", StringComparison.OrdinalIgnoreCase)
               || string.Equals(errorCode, "ACCESS_DENIED", StringComparison.OrdinalIgnoreCase);
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

    private static string SummarizeStandardError(string? standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return "n/a";
        }

        var normalized = standardError
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        const int maxLength = 280;
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string BuildConnectivityHint(string mappedErrorCode, string? standardError)
    {
        if (string.Equals(mappedErrorCode, "ACCESS_DENIED", StringComparison.OrdinalIgnoreCase))
        {
            return "Kimlik/yetkiyi kontrol et (service account, local admin, WinRM auth).";
        }

        if (string.Equals(mappedErrorCode, "TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return "Timeout/Firewall/Route kaynakli olabilir (5985/5986, DNS, proxy, ACL).";
        }

        if (string.Equals(mappedErrorCode, "WINRM_CONNECT_FAILED", StringComparison.OrdinalIgnoreCase))
        {
            if (IsPossibleFirewallOrNetworkBlock(standardError))
            {
                return "Olasi firewall/network blokaji (5985/5986, host ACL, route).";
            }

            return "WinRM endpoint/service, DNS ve sertifika ayarlarini kontrol et.";
        }

        return "Standart hata ciktisini incele.";
    }

    private static bool IsPossibleFirewallOrNetworkBlock(string? standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return false;
        }

        return standardError.Contains("timed out", StringComparison.OrdinalIgnoreCase)
               || standardError.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
               || standardError.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase)
               || standardError.Contains("No such host is known", StringComparison.OrdinalIgnoreCase)
               || standardError.Contains("The client cannot connect", StringComparison.OrdinalIgnoreCase)
               || standardError.Contains("destination specified", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9.-]{0,253}$")]
    private static partial Regex ServerNamePattern();
}
