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
        endpoint.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
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
