namespace Infrastructure.Messaging;

/// <summary>
/// Worker tarafinda concurrency ve prefetch ayarlari.
/// </summary>
public sealed class ConsumerOptions
{
    public ushort PrefetchCount { get; set; } = 50;
    public int ConcurrencyLimit { get; set; } = 50;

    /// <summary>
    /// Consumer-level retry (poison message -> error queue).
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    public int RetryIntervalSeconds { get; set; } = 5;

    public KillSwitchOptions KillSwitch { get; set; } = new();

    /// <summary>
    /// Endpoint bazli tuning override'lari.
    /// Ornek anahtarlar: Discovery, PasswordChange, Update, Verify, ResultEvents.
    /// </summary>
    public Dictionary<string, ConsumerEndpointOverrideOptions> EndpointOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ConsumerEndpointOverrideOptions
{
    public ushort? PrefetchCount { get; set; }
    public int? ConcurrencyLimit { get; set; }
    public int? RetryAttempts { get; set; }
    public int? RetryIntervalSeconds { get; set; }
    public KillSwitchOverrideOptions? KillSwitch { get; set; }
}

public sealed class KillSwitchOverrideOptions
{
    public bool? Enabled { get; set; }
    public int? TrackingPeriodSeconds { get; set; }
    public int? ActivationThreshold { get; set; }
    public int? TripThreshold { get; set; }
    public int? RestartTimeoutSeconds { get; set; }
}
