using Application.Abstractions.Repositories;
using Application.Models;
using Application.Services.Rbac;
using Application.UseCases.Admin.Models;
using Domain.Entities;

namespace Application.UseCases.Admin;

public sealed class SetUserRolesUseCase
{
    private readonly IIdentityRepository _identityRepository;

    public SetUserRolesUseCase(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository;
    }

    public async Task<UseCaseResult<SetUserRolesOutput>> ExecuteAsync(
        SetUserRolesInput input,
        CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            return UseCaseResult<SetUserRolesOutput>.Failure("INVALID_USERID", "UserId gecersiz.");
        }

        var normalizedRoles = NormalizeRoles(input.Roles);
        if (!normalizedRoles.IsSuccess)
        {
            return UseCaseResult<SetUserRolesOutput>.Failure(normalizedRoles.ErrorCode!, normalizedRoles.ErrorMessage!);
        }

        var desiredRoleNames = normalizedRoles.Value!;

        var user = await _identityRepository.GetUserByIdWithRolesAsync(input.UserId, cancellationToken);
        if (user is null)
        {
            return UseCaseResult<SetUserRolesOutput>.Failure("USER_NOT_FOUND", "Kullanici bulunamadi.");
        }

        if (user.IsActive)
        {
            var currentlyAdmin = user.UserRoles.Any(userRole =>
                userRole.Role.Name == KnownRoles.Admin || userRole.Role.Name == KnownRoles.SuperAdmin);
            var desiredAdmin = desiredRoleNames.Contains(KnownRoles.Admin, StringComparer.Ordinal)
                               || desiredRoleNames.Contains(KnownRoles.SuperAdmin, StringComparer.Ordinal);

            if (currentlyAdmin && !desiredAdmin)
            {
                var hasOtherAdmin = await _identityRepository.ExistsOtherActiveAdminAsync(user.Id, cancellationToken);
                if (!hasOtherAdmin)
                {
                    return UseCaseResult<SetUserRolesOutput>.Failure("LAST_ADMIN", "Son admin'in yetkisi kaldirilamaz.");
                }
            }
        }

        var desiredRoles = await _identityRepository.GetRolesByNamesAsync(desiredRoleNames, cancellationToken);
        var desiredRoleNameSet = new HashSet<string>(desiredRoleNames, StringComparer.Ordinal);
        var resolvedRoleNameSet = new HashSet<string>(desiredRoles.Select(role => role.Name), StringComparer.Ordinal);
        var missing = desiredRoleNameSet.Except(resolvedRoleNameSet, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            return UseCaseResult<SetUserRolesOutput>.Failure("ROLE_NOT_FOUND", "Rol tablosu eksik. RBAC seed/calismasi gerekli.");
        }

        var desiredRoleIdSet = desiredRoles.Select(role => role.Id).ToHashSet();

        // Remove
        var toRemove = user.UserRoles
            .Where(userRole => !desiredRoleIdSet.Contains(userRole.RoleId))
            .ToList();

        foreach (var item in toRemove)
        {
            user.UserRoles.Remove(item);
        }

        // Add
        var existingRoleIdSet = user.UserRoles.Select(userRole => userRole.RoleId).ToHashSet();
        var toAdd = desiredRoles.Where(role => !existingRoleIdSet.Contains(role.Id)).ToList();

        foreach (var role in toAdd)
        {
            user.UserRoles.Add(new AppUserRole
            {
                AppUserId = user.Id,
                RoleId = role.Id
            });
        }

        await _identityRepository.SaveChangesAsync(cancellationToken);

        return UseCaseResult<SetUserRolesOutput>.Success(new SetUserRolesOutput
        {
            UserId = user.Id,
            Subject = user.Subject,
            Roles = desiredRoleNames
        });
    }

    private static UseCaseResult<IReadOnlyCollection<string>> NormalizeRoles(IReadOnlyCollection<string> roles)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in roles)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            if (!KnownRoles.TryNormalize(trimmed, out var normalized))
            {
                return UseCaseResult<IReadOnlyCollection<string>>.Failure("INVALID_ROLE", $"Gecersiz rol: {trimmed}");
            }

            set.Add(normalized);
        }

        return UseCaseResult<IReadOnlyCollection<string>>.Success(set.OrderBy(x => x).ToArray());
    }
}
