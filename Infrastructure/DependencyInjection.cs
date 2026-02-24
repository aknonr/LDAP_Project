using Application.Abstractions.Messaging;
using Application.Services.Rbac;
using Application.Abstractions.Auditing;
using Application.Abstractions.Directory;
using Application.Abstractions.Discovery;
using Application.Abstractions.Inventory;
using Application.Abstractions.Repositories;
using Application.Abstractions.Security;
using Application.Abstractions.Tracking;
using Application.Abstractions.Update;
using Application.Abstractions.Verify;
using Infrastructure.Identity;
using Infrastructure.Directory;
using Infrastructure.Discovery;
using Infrastructure.Discovery.Strategies;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Auditing;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Persistence.Tracking;
using Infrastructure.RemoteExecution;
using Infrastructure.Security;
using Infrastructure.ThApi;
using Infrastructure.Update;
using Infrastructure.Update.Strategies;
using Infrastructure.Verify;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Infrastructure.Concurrency;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<AdpmDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Payload sifreleme ayarlarini configten alir.
        var payloadSection = configuration.GetSection("Security:PayloadEncryption");
        services.Configure<PayloadEncryptionOptions>(options =>
        {
            options.Algorithm = payloadSection["Algorithm"] ?? "AES-GCM";
            options.KeyId = payloadSection["KeyId"] ?? string.Empty;
            options.SharedKeyBase64 = payloadSection["SharedKeyBase64"];
        });

        services.Configure<LdapOptions>(configuration.GetSection("Ldap"));
        services.Configure<RemoteExecutionOptions>(configuration.GetSection("RemoteExecution"));

        services.Configure<ThApiOptions>(configuration.GetSection("ThApi"));
        services.AddHttpClient<IThApiClient, ThApiClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<ThApiOptions>>().CurrentValue;
            if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 15 : options.TimeoutSeconds);
        });

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IServerGroupRepository, ServerGroupRepository>();
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<ICommandPublisher, MassTransitCommandPublisher>();
        services.AddScoped<IPayloadProtector, AesGcmPayloadProtector>();
        services.AddScoped<IRoleResolver, RoleResolver>();
        services.AddScoped<IAdPasswordChangeService, AdPasswordChangeService>();
        services.AddScoped<IJobTrackingService, JobTrackingService>();
        services.AddScoped<IAuditTrailWriter, AuditTrailStore>();
        services.AddScoped<IAuditTrailReader, AuditTrailStore>();
        services.AddScoped<IInventorySyncService, InventorySyncService>();
        services.AddScoped<IDistributedLeaseManager, SqlDistributedLeaseManager>();
        services.AddScoped<IRemoteCommandExecutor, PowerShellWinRmCommandExecutor>();
        services.AddScoped<IDiscoveryEngine, DiscoveryEngine>();
        services.AddScoped<IUpdateEngine, UpdateEngine>();
        services.AddScoped<IVerifyEngine, VerifyEngine>();
        services.AddScoped<IDiscoveryStrategy, ServiceDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, ScheduledTaskDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, IisAppPoolDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, IisSiteDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, IisWebAppDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, IisVirtualDirDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, ComPlusDiscoveryStrategy>();
        services.AddScoped<IDiscoveryStrategy, UserRightDiscoveryStrategy>();
        services.AddScoped<IUpdateStrategy, ServiceUpdateStrategy>();
        services.AddScoped<IUpdateStrategy, ScheduledTaskUpdateStrategy>();
        services.AddScoped<IUpdateStrategy, IisAppPoolUpdateStrategy>();
        services.AddScoped<IUpdateStrategy, IisSiteUpdateStrategy>();
        services.AddScoped<IUpdateStrategy, IisWebAppUpdateStrategy>();
        services.AddScoped<IUpdateStrategy, IisVirtualDirUpdateStrategy>();
        services.AddScoped<IUpdateStrategy, ComPlusUpdateStrategy>();

        return services;
    }
}
