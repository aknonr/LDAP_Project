using Application.Messaging;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Options;
using Serilog;
using Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// RabbitMQ ve concurrency ayarlarini configten oku.
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("Messaging:RabbitMq"));
builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection("Messaging:Consumer"));

// MassTransit consumer konfigurasyonu.
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<DiscoverServerUsageConsumer>();
    configurator.AddConsumer<UpdateServerResourcesConsumer>();
    configurator.AddConsumer<VerifyServerConsumer>();

    configurator.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var consumerOptions = context.GetRequiredService<IOptions<ConsumerOptions>>().Value;

        cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
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

        cfg.ReceiveEndpoint(QueueNames.ServerDiscoveryCommands, endpoint =>
        {
            endpoint.PrefetchCount = consumerOptions.PrefetchCount;
            endpoint.UseConcurrencyLimit(consumerOptions.ConcurrencyLimit);
            endpoint.ConfigureConsumer<DiscoverServerUsageConsumer>(context);
        });

        cfg.ReceiveEndpoint(QueueNames.ServerUpdateCommands, endpoint =>
        {
            endpoint.PrefetchCount = consumerOptions.PrefetchCount;
            endpoint.UseConcurrencyLimit(consumerOptions.ConcurrencyLimit);
            endpoint.ConfigureConsumer<UpdateServerResourcesConsumer>(context);
        });

        cfg.ReceiveEndpoint(QueueNames.ServerVerifyCommands, endpoint =>
        {
            endpoint.PrefetchCount = consumerOptions.PrefetchCount;
            endpoint.UseConcurrencyLimit(consumerOptions.ConcurrencyLimit);
            endpoint.ConfigureConsumer<VerifyServerConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<Worker.Worker>();
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
