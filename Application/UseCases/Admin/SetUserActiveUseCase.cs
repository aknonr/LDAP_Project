using Application.Abstractions.Repositories;
using Application.Models;
using Application.Services.Rbac;
using Application.UseCases.Admin.Models;

namespace Application.UseCases.Admin;

public sealed class SetUserActiveUseCase
{
    private readonly IIdentityRepository _identityRepository;

    public SetUserActiveUseCase(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository;
    }

    public async Task<UseCaseResult<SetUserActiveOutput>> ExecuteAsync(
        SetUserActiveInput input,
        CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            return UseCaseResult<SetUserActiveOutput>.Failure("INVALID_USERID", "UserId gecersiz.");
        }

        var user = await _identityRepository.GetUserByIdWithRolesAsync(input.UserId, cancellationToken);
        if (user is null)
        {
            return UseCaseResult<SetUserActiveOutput>.Failure("USER_NOT_FOUND", "Kullanici bulunamadi.");
        }

        if (!input.IsActive && user.IsActive)
        {
            var isAdmin = user.UserRoles.Any(userRole =>
                userRole.Role.Name == KnownRoles.Admin || userRole.Role.Name == KnownRoles.SuperAdmin);

            if (isAdmin)
            {
                var hasOtherAdmin = await _identityRepository.ExistsOtherActiveAdminAsync(user.Id, cancellationToken);
                if (!hasOtherAdmin)
                {
                    return UseCaseResult<SetUserActiveOutput>.Failure("LAST_ADMIN", "Son admin pasif edilemez.");
                }
            }
        }

        user.IsActive = input.IsActive;
        await _identityRepository.SaveChangesAsync(cancellationToken);

        return UseCaseResult<SetUserActiveOutput>.Success(new SetUserActiveOutput
        {
            UserId = user.Id,
            Subject = user.Subject,
            IsActive = user.IsActive
        });
    }
}
