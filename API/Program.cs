using API.Auth;
using API.Consumers;
using API.Hubs;
using API.Logging;
using Application.Messaging;
using Application.Messaging.Events;
using Application.UseCases.Jobs;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var swaggerTitle = builder.Configuration.GetValue<string>("Swagger:Title") ?? "EnterpriseADPasswordManager API";
var swaggerVersion = builder.Configuration.GetValue<string>("Swagger:Version") ?? "v1";
var signalRHubPath = builder.Configuration.GetValue<string>("Realtime:SignalR:HubPath") ?? "/hubs/jobs";
var signalRMaxMessageSizeKb = builder.Configuration.GetValue<long?>("Realtime:SignalR:MaximumReceiveMessageSizeKb") ?? 64;
var signalRDetailedErrors = builder.Configuration.GetValue<bool>("Realtime:SignalR:EnableDetailedErrors");

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(swaggerVersion, new OpenApiInfo
    {
        Title = swaggerTitle,
        Version = swaggerVersion,
        Description = "Enterprise AD Password Manager API"
    });

    options.SupportNonNullableReferenceTypes();
    options.UseAllOfToExtendReferenceSchemas();
    options.UseInlineDefinitionsForEnums();
    options.CustomSchemaIds(type => type.FullName);
    options.MapType<DateOnly>(() => new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" });
    options.MapType<TimeOnly>(() => new OpenApiSchema { Type = JsonSchemaType.String, Format = "time" });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token} formatinda JWT giriniz."
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(document =>
    {
        var bearerSchemeReference = new OpenApiSecuritySchemeReference("Bearer", document, null);

        return new OpenApiSecurityRequirement
        {
            { bearerSchemeReference, new List<string>() }
        };
    });
});

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("Messaging:RabbitMq"));
builder.Services.Configure<ConsumerOptions>(builder.Configuration.GetSection("Messaging:Consumer"));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection("Auth:Oidc"));

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = signalRDetailedErrors;
    options.MaximumReceiveMessageSize = signalRMaxMessageSizeKb * 1024;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var oidc = builder.Configuration.GetSection("Auth:Oidc").Get<OidcOptions>() ?? new OidcOptions();

        options.Authority = oidc.Authority;
        options.Audience = string.IsNullOrWhiteSpace(oidc.Audience) ? null : oidc.Audience;
        options.RequireHttpsMetadata = oidc.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = oidc.Authority,
            ValidateAudience = !string.IsNullOrWhiteSpace(oidc.Audience),
            ValidAudience = string.IsNullOrWhiteSpace(oidc.Audience) ? null : oidc.Audience,
            NameClaimType = "name",
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new AllowedGroupRequirement())
        .Build();
});

builder.Services.AddSingleton<IAuthorizationHandler, AllowedGroupHandler>();
builder.Services.AddScoped<IClaimsTransformation, RbacClaimsTransformation>();

// Job use-case'lerini API katmaninda is akisi olarak kaydeder.
builder.Services.AddScoped<CreateDiscoveryJobUseCase>();
builder.Services.AddScoped<CreatePasswordChangeJobUseCase>();
builder.Services.AddScoped<GetJobStatusUseCase>();
builder.Services.AddScoped<GetJobTargetsUseCase>();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<ServerUsageResultEventConsumer>();
    configurator.AddConsumer<ServerUpdateResultEventConsumer>();
    configurator.AddConsumer<JobProgressEventConsumer>();

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

        // Result event'lerini sabit exchange adinda toplar.
        cfg.Message<ServerUsageResultEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
        cfg.Message<ServerUpdateResultEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
        cfg.Message<JobProgressEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));

        // API tarafi result event queue'sunu tuketir ve SignalR'a aktarir.
        cfg.ReceiveEndpoint(QueueNames.ServerResultEvents, endpoint =>
        {
            endpoint.PrefetchCount = consumerOptions.PrefetchCount;
            endpoint.UseConcurrencyLimit(consumerOptions.ConcurrencyLimit);
            endpoint.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
            endpoint.ConfigureConsumer<ServerUsageResultEventConsumer>(context);
            endpoint.ConfigureConsumer<ServerUpdateResultEventConsumer>(context);
            endpoint.ConfigureConsumer<JobProgressEventConsumer>(context);
        });
    });
});

// Command mesajlarini dogrudan queue'lara gondermek icin URI map'leri.
EndpointConvention.Map<Application.Messaging.Commands.DiscoverServerUsageCommand>(
    new Uri($"queue:{QueueNames.ServerDiscoveryCommands}"));
EndpointConvention.Map<Application.Messaging.Commands.UpdateServerResourcesCommand>(
    new Uri($"queue:{QueueNames.ServerUpdateCommands}"));
EndpointConvention.Map<Application.Messaging.Commands.VerifyServerCommand>(
    new Uri($"queue:{QueueNames.ServerVerifyCommands}"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", $"{swaggerTitle} {swaggerVersion}");
        options.DocumentTitle = $"{swaggerTitle} Docs";
        options.DisplayRequestDuration();
        options.DocExpansion(DocExpansion.None);
        options.EnableTryItOutByDefault();
        options.ConfigObject.PersistAuthorization = true;
    });
}
else
{
    app.UseHsts();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("CorrelationId", CorrelationIdAccessor.Get(httpContext) ?? httpContext.TraceIdentifier);
        diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name ?? "anonymous");
    };
});
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<JobsHub>(signalRHubPath);

app.Run();
