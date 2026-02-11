namespace Infrastructure.Messaging;

/// <summary>
/// Worker tarafinda concurrency ve prefetch ayarlari.
/// </summary>
public sealed class ConsumerOptions
{
    public ushort PrefetchCount { get; set; } = 50;
    public int ConcurrencyLimit { get; set; } = 50;
}
