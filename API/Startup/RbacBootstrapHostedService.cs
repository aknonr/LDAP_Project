using API.Auth;
using Application.Services.Rbac;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace API.Startup;

/// <summary>
/// Ilk kurulumda RBAC tablolarini hazirlamak icin baslangic seeding/bootstrapping.
/// Not: Prod ortamda bootstrap sadece kontrollu sekilde ve gecici olarak acilmalidir.
/// </summary>
public sealed class RbacBootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BootstrapOptions> _bootstrapOptions;
    private readonly ILogger<RbacBootstrapHostedService> _logger;

    public RbacBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BootstrapOptions> bootstrapOptions,
        ILogger<RbacBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _bootstrapOptions = bootstrapOptions;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdpmDbContext>();

        await EnsureRolesAsync(dbContext, cancellationToken);

        var options = _bootstrapOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        var subject = (options.InitialAdminSubject ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            _logger.LogWarning("Auth:Bootstrap:Enabled=true ama InitialAdminSubject bos. Bootstrap admin atlandi.");
            return;
        }

        var hasActiveAdmin = await dbContext.AppUsers
            .AsNoTracking()
            .AnyAsync(
                user => user.IsActive &&
                        user.UserRoles.Any(userRole =>
                            userRole.Role.Name == KnownRoles.Admin || userRole.Role.Name == KnownRoles.SuperAdmin),
                cancellationToken);

        if (hasActiveAdmin)
        {
            _logger.LogInformation("Bootstrap admin atlandi (zaten aktif admin mevcut).");
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(options.InitialAdminDisplayName)
            ? subject
            : options.InitialAdminDisplayName.Trim();

        var email = string.IsNullOrWhiteSpace(options.InitialAdminEmail) ? null : options.InitialAdminEmail.Trim();

        var userEntity = await dbContext.AppUsers
            .Include(user => user.UserRoles)
            .FirstOrDefaultAsync(user => user.Subject == subject, cancellationToken);

        if (userEntity is null)
        {
            userEntity = new AppUser
            {
                Id = Guid.NewGuid(),
                Subject = subject,
                DisplayName = displayName,
                Email = email,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.AppUsers.Add(userEntity);
        }
        else
        {
            userEntity.IsActive = true;
            userEntity.DisplayName = displayName;
            userEntity.Email = email;
        }

        var superAdminRole = await dbContext.Roles
            .FirstOrDefaultAsync(role => role.Name == KnownRoles.SuperAdmin, cancellationToken);

        if (superAdminRole is null)
        {
            superAdminRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = KnownRoles.SuperAdmin
            };

            dbContext.Roles.Add(superAdminRole);
        }

        if (!userEntity.UserRoles.Any(item => item.RoleId == superAdminRole.Id))
        {
            userEntity.UserRoles.Add(new AppUserRole
            {
                AppUserId = userEntity.Id,
                RoleId = superAdminRole.Id
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Bootstrap admin olusturuldu/aktiflestirildi. Subject={Subject}", subject);
        }
        catch (DbUpdateException ex)
        {
            // Paralel startup durumunda duplicate insert'ler unique constraint'e takilabilir.
            // Bu durumda seed zaten baska instance tarafindan yapilmis olabilir.
            _logger.LogWarning(ex, "Bootstrap admin seed SaveChanges hatasi. Subject={Subject}", subject);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureRolesAsync(AdpmDbContext dbContext, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Roles
            .AsNoTracking()
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var missing = KnownRoles.All.Where(roleName => !existingSet.Contains(roleName)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var roleName in missing)
        {
            dbContext.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Paralel startup durumunda ignore.
        }
    }
}

