using Application.Services.Rbac;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public sealed class RoleResolver : IRoleResolver
{
    private readonly AdpmDbContext _dbContext;

    public RoleResolver(AdpmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> GetRolesForSubjectAsync(string subject, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Array.Empty<string>();
        }

        var roles = await _dbContext.AppUsers
            .AsNoTracking()
            .Where(user => user.Subject == subject && user.IsActive)
            .SelectMany(user => user.UserRoles.Select(userRole => userRole.Role.Name))
            .Distinct()
            .ToListAsync(cancellationToken);

        return roles;
    }
}
