namespace Worker.Observability;

public interface IQueueLagProbe
{
    Task<QueueLagSample?> TryReadAsync(
        string queueName,
        string virtualHost,
        CancellationToken cancellationToken);
}

public sealed record QueueLagSample(
    string QueueName,
    int MessagesReady,
    int MessagesUnacknowledged,
    DateTimeOffset CollectedAtUtc);
