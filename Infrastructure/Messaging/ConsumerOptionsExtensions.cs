namespace Infrastructure.Messaging;

public static class ConsumerOptionsExtensions
{
    public static ConsumerOptions ResolveForEndpoint(this ConsumerOptions source, string endpointKey)
    {
        var resolved = Clone(source);
        if (string.IsNullOrWhiteSpace(endpointKey))
        {
            return resolved;
        }

        if (!source.EndpointOverrides.TryGetValue(endpointKey.Trim(), out var endpointOverride))
        {
            return resolved;
        }

        if (endpointOverride.PrefetchCount is { } prefetchCount)
        {
            resolved.PrefetchCount = prefetchCount;
        }

        if (endpointOverride.ConcurrencyLimit is { } concurrencyLimit)
        {
            resolved.ConcurrencyLimit = concurrencyLimit;
        }

        if (endpointOverride.RetryAttempts is { } retryAttempts)
        {
            resolved.RetryAttempts = retryAttempts;
        }

        if (endpointOverride.RetryIntervalSeconds is { } retryIntervalSeconds)
        {
            resolved.RetryIntervalSeconds = retryIntervalSeconds;
        }

        if (endpointOverride.KillSwitch is null)
        {
            return resolved;
        }

        if (endpointOverride.KillSwitch.Enabled is { } killSwitchEnabled)
        {
            resolved.KillSwitch.Enabled = killSwitchEnabled;
        }

        if (endpointOverride.KillSwitch.TrackingPeriodSeconds is { } trackingPeriodSeconds)
        {
            resolved.KillSwitch.TrackingPeriodSeconds = trackingPeriodSeconds;
        }

        if (endpointOverride.KillSwitch.ActivationThreshold is { } activationThreshold)
        {
            resolved.KillSwitch.ActivationThreshold = activationThreshold;
        }

        if (endpointOverride.KillSwitch.TripThreshold is { } tripThreshold)
        {
            resolved.KillSwitch.TripThreshold = tripThreshold;
        }

        if (endpointOverride.KillSwitch.RestartTimeoutSeconds is { } restartTimeoutSeconds)
        {
            resolved.KillSwitch.RestartTimeoutSeconds = restartTimeoutSeconds;
        }

        return resolved;
    }

    private static ConsumerOptions Clone(ConsumerOptions source)
    {
        return new ConsumerOptions
        {
            PrefetchCount = source.PrefetchCount,
            ConcurrencyLimit = source.ConcurrencyLimit,
            RetryAttempts = source.RetryAttempts,
            RetryIntervalSeconds = source.RetryIntervalSeconds,
            KillSwitch = new KillSwitchOptions
            {
                Enabled = source.KillSwitch.Enabled,
                TrackingPeriodSeconds = source.KillSwitch.TrackingPeriodSeconds,
                ActivationThreshold = source.KillSwitch.ActivationThreshold,
                TripThreshold = source.KillSwitch.TripThreshold,
                RestartTimeoutSeconds = source.KillSwitch.RestartTimeoutSeconds
            }
        };
    }
}
