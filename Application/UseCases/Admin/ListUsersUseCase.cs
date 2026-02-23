using Application.Abstractions.Repositories;
using Application.Models;
using Application.UseCases.Admin.Models;

namespace Application.UseCases.Admin;

public sealed class ListUsersUseCase
{
    private const int MaxTake = 500;
    private readonly IIdentityRepository _identityRepository;

    public ListUsersUseCase(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository;
    }

    public async Task<UseCaseResult<ListUsersOutput>> ExecuteAsync(
        ListUsersInput input,
        CancellationToken cancellationToken)
    {
        if (input.Skip < 0)
        {
            return UseCaseResult<ListUsersOutput>.Failure("INVALID_SKIP", "Skip 0 veya daha buyuk olmalidir.");
        }

        if (input.Take <= 0 || input.Take > MaxTake)
        {
            return UseCaseResult<ListUsersOutput>.Failure("INVALID_TAKE", $"Take 1-{MaxTake} araliginda olmalidir.");
        }

        var totalCount = await _identityRepository.GetUserCountAsync(cancellationToken);
        var users = await _identityRepository.ListUsersAsync(input.Skip, input.Take, cancellationToken);

        var items = users.Select(user => new UserListItem
        {
            UserId = user.UserId,
            Subject = user.Subject,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.Roles
        }).ToList();

        return UseCaseResult<ListUsersOutput>.Success(new ListUsersOutput
        {
            TotalCount = totalCount,
            Items = items
        });
    }
}

