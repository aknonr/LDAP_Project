using API.Auth;
using Application.Messaging;
using Application.Messaging.Events;
using Infrastructure;
using Infrastructure.Messaging;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
var swaggerTitle = builder.Configuration.GetValue<string>("Swagger:Title") ?? "EnterpriseADPasswordManager API";
var swaggerVersion = builder.Configuration.GetValue<string>("Swagger:Version") ?? "v1";

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

// RabbitMQ baglanti ayarlarini configten oku.
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("Messaging:RabbitMq"));

// MassTransit bus konfigurasyonu (API tarafinda sadece publish).
builder.Services.AddMassTransit(configurator =>
{
    configurator.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

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

        // Event exchange adini sabitle (server.result.events).
        cfg.Message<ServerUsageResultEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
        cfg.Message<ServerUpdateResultEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
        cfg.Message<JobProgressEvent>(message => message.SetEntityName(QueueNames.ServerResultEvents));
    });
});

// Command mesajlarini dogrudan queue'lara gondermek icin URI map'leri.
EndpointConvention.Map<Application.Messaging.Commands.DiscoverServerUsageCommand>(
    new Uri($"queue:{QueueNames.ServerDiscoveryCommands}"));
EndpointConvention.Map<Application.Messaging.Commands.UpdateServerResourcesCommand>(
    new Uri($"queue:{QueueNames.ServerUpdateCommands}"));
EndpointConvention.Map<Application.Messaging.Commands.VerifyServerCommand>(
    new Uri($"queue:{QueueNames.ServerVerifyCommands}"));

builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection("Auth:Oidc"));

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

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
