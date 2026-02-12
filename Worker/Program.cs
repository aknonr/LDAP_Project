using Application.Messaging;
using Application.Messaging.Events;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Options;
using Serilog;
using Worker.Configuration;
using Worker.Consumers;
using Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("Messaging:RabbitMq"));
builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection("Messaging:Consumer"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Messaging:Outbox"));
builder.Services.Configure<WorkerRoleOptions>(builder.Configuration.GetSection("WorkerRoles"));
builder.Services.Configure<VerificationOptions>(builder.Configuration.GetSection("Verification"));

var workerRoles = builder.Configuration.GetSection("WorkerRoles").Get<WorkerRoleOptions>() ?? new WorkerRoleOptions();
var outboxOptions = builder.Configuration.GetSection("Messaging:Outbox").Get<OutboxOptions>() ?? new OutboxOptions();

builder.Services.AddMassTransit(configurator =>
{
    if (outboxOptions.Enabled)
    {
        configurator.AddEntityFrameworkOutbox<Infrastructure.Persistence.AdpmDbContext>(outbox =>
        {
            outbox.UseSqlServer();
            outbox.QueryDelay = TimeSpan.FromSeconds(Math.Max(1, outboxOptions.QueryDelaySeconds));

            if (outboxOptions.UseBusOutbox)
            {
                outbox.UseBusOutbox();
            }
        });
    }

    if (workerRoles.EnableDiscovery)
    {
        configurator.AddConsumer<DiscoverServerUsageConsumer>();
    }

    if (workerRoles.EnableUpdate)
    {
        configurator.AddConsumer<UpdateServerResourcesConsumer>();
    }

    if (workerRoles.EnableVerify)
    {
        configurator.AddConsumer<VerifyServerConsumer>();
    }

    configurator.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var consumerOptions = context.GetRequiredService<IOptions<ConsumerOptions>>().Value;

        cfg.ConfigureHostDefaults(options);

        cfg.Message<ServerUsageResultEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
        cfg.Message<ServerUpdateResultEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
        cfg.Message<JobProgressEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));

        if (workerRoles.EnableDiscovery)
        {
            cfg.ReceiveEndpoint(QueueNames.ServerDiscoveryCommands, endpoint =>
            {
                endpoint.ConfigureEndpointDefaults(consumerOptions, options);
                endpoint.UseEntityFrameworkOutbox<Infrastructure.Persistence.AdpmDbContext>(context);
                endpoint.ConfigureConsumer<DiscoverServerUsageConsumer>(context);
            });
        }

        if (workerRoles.EnableUpdate)
        {
            cfg.ReceiveEndpoint(QueueNames.ServerUpdateCommands, endpoint =>
            {
                endpoint.ConfigureEndpointDefaults(consumerOptions, options);
                endpoint.UseEntityFrameworkOutbox<Infrastructure.Persistence.AdpmDbContext>(context);
                endpoint.ConfigureConsumer<UpdateServerResourcesConsumer>(context);
            });
        }

        if (workerRoles.EnableVerify)
        {
            cfg.ReceiveEndpoint(QueueNames.ServerVerifyCommands, endpoint =>
            {
                endpoint.ConfigureEndpointDefaults(consumerOptions, options);
                endpoint.UseEntityFrameworkOutbox<Infrastructure.Persistence.AdpmDbContext>(context);
                endpoint.ConfigureConsumer<VerifyServerConsumer>(context);
            });
        }
    });
});

builder.Services.AddHostedService<Worker.Worker>();
if (workerRoles.EnableInventorySync)
{
    builder.Services.AddHostedService<InventorySyncJob>();
}

builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
