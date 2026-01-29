using Infrastructure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddHostedService<Worker.Worker>();
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
