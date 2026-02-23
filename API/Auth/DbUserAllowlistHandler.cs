using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Auth;

public sealed class DbUserAllowlistHandler : AuthorizationHandler<DbUserAllowlistRequirement>
{
    private readonly AdpmDbContext _dbContext;
    private readonly ILogger<DbUserAllowlistHandler> _logger;

    public DbUserAllowlistHandler(AdpmDbContext dbContext, ILogger<DbUserAllowlistHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DbUserAllowlistRequirement requirement)
    {
        var subject = context.User.FindFirstValue("sub")
                      ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return;
        }

        try
        {
            var exists = await _dbContext.AppUsers
                .AsNoTracking()
                .AnyAsync(user => user.Subject == subject && user.IsActive);

            if (exists)
            {
                context.Succeed(requirement);
            }
        }
        catch (Exception ex)
        {
            // Fail-closed: DB okunamiyorsa allowlist kontrolu gecemez.
            _logger.LogError(ex, "DB allowlist kontrolu hata verdi. Subject={Subject}", subject);
        }
    }
}

