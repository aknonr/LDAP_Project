using Application.Abstractions.Repositories;
using Application.Services.Rbac;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly AdpmDbContext _dbContext;

    public IdentityRepository(AdpmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AppUser?> GetUserByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<AppUser?> GetUserBySubjectWithRolesAsync(string subject, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.Subject == subject, cancellationToken);
    }

    public Task AddUserAsync(AppUser user, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers.AddAsync(user, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<UserListItemReadModel>> ListUsersAsync(int skip, int take, CancellationToken cancellationToken)
    {
        // Listede tracking gereksiz: projeksiyon + AsNoTracking.
        return await _dbContext.AppUsers
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Subject)
            .Skip(skip)
            .Take(take)
            .Select(user => new UserListItemReadModel
            {
                UserId = user.Id,
                Subject = user.Subject,
                DisplayName = user.DisplayName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles
                    .Select(userRole => userRole.Role.Name)
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetUserCountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllRoleNamesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        if (roleNames.Count == 0)
        {
            return Array.Empty<Role>();
        }

        return await _dbContext.Roles
            .Where(role => roleNames.Contains(role.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsOtherActiveAdminAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        return _dbContext.AppUsers
            .AsNoTracking()
            .Where(user => user.IsActive && user.Id != excludedUserId)
            .AnyAsync(
                user => user.UserRoles.Any(userRole =>
                    userRole.Role.Name == KnownRoles.Admin || userRole.Role.Name == KnownRoles.SuperAdmin),
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
