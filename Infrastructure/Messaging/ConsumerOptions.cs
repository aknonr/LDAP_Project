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
}
