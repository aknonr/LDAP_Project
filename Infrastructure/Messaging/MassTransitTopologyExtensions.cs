using MassTransit;

namespace Infrastructure.Messaging;

public static class MassTransitTopologyExtensions
{
    public static void ConfigureHostDefaults(this IRabbitMqBusFactoryConfigurator configurator, RabbitMqOptions options)
    {
        configurator.Host(options.Host, options.Port, options.VirtualHost, host =>
        {
            host.Username(options.Username);
            host.Password(options.Password);
            host.Heartbeat(options.RequestedHeartbeat);

            if (options.UseTls)
            {
                host.UseSsl(ssl =>
                {
                    ssl.ServerName = string.IsNullOrWhiteSpace(options.SslServerName)
                        ? options.Host
                        : options.SslServerName;
                });
            }
        });
    }

    public static void ConfigureEndpointDefaults(
        this IRabbitMqReceiveEndpointConfigurator endpoint,
        ConsumerOptions consumerOptions,
        RabbitMqOptions rabbitMqOptions)
    {
        endpoint.PrefetchCount = consumerOptions.PrefetchCount;
        endpoint.UseConcurrencyLimit(consumerOptions.ConcurrencyLimit);
        endpoint.UseMessageRetry(retry =>
            retry.Interval(
                Math.Max(0, consumerOptions.RetryAttempts),
                TimeSpan.FromSeconds(Math.Max(1, consumerOptions.RetryIntervalSeconds))));

        if (consumerOptions.KillSwitch.Enabled)
        {
            endpoint.UseKillSwitch(options =>
            {
                options.TrackingPeriod = TimeSpan.FromSeconds(Math.Max(10, consumerOptions.KillSwitch.TrackingPeriodSeconds));
                options.TripThreshold = Math.Clamp(consumerOptions.KillSwitch.TripThreshold, 1, 100);
                options.ActivationThreshold = Math.Max(1, consumerOptions.KillSwitch.ActivationThreshold);
                options.RestartTimeout = TimeSpan.FromSeconds(Math.Max(10, consumerOptions.KillSwitch.RestartTimeoutSeconds));
            });
        }

        endpoint.Durable = true;
        endpoint.AutoDelete = false;

        if (rabbitMqOptions.UseQuorumQueues)
        {
            if (rabbitMqOptions.QuorumReplicationFactor is > 0)
            {
                endpoint.SetQuorumQueue(rabbitMqOptions.QuorumReplicationFactor.Value);
            }
            else
            {
                endpoint.SetQuorumQueue();
            }
        }
    }
}
