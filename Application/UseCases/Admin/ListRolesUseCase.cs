using Application.Abstractions.Repositories;
using Application.Models;
using Application.Services.Rbac;
using Application.UseCases.Admin.Models;

namespace Application.UseCases.Admin;

public sealed class ListRolesUseCase
{
    private readonly IIdentityRepository _identityRepository;

    public ListRolesUseCase(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository;
    }

    public async Task<UseCaseResult<ListRolesOutput>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var roles = await _identityRepository.GetAllRoleNamesAsync(cancellationToken);
        var result = roles.Count > 0 ? roles : KnownRoles.All;

        return UseCaseResult<ListRolesOutput>.Success(new ListRolesOutput
        {
            Roles = result
        });
    }
}

