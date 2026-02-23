using Application.Abstractions.Repositories;
using Application.Models;
using Application.UseCases.Admin.Models;
using Domain.Entities;

namespace Application.UseCases.Admin;

public sealed class UpsertUserUseCase
{
    private readonly IIdentityRepository _identityRepository;

    public UpsertUserUseCase(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository;
    }

    public async Task<UseCaseResult<UpsertUserOutput>> ExecuteAsync(
        UpsertUserInput input,
        CancellationToken cancellationToken)
    {
        var subject = (input.Subject ?? string.Empty).Trim();
        var displayName = (input.DisplayName ?? string.Empty).Trim();
        var email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();

        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 200)
        {
            return UseCaseResult<UpsertUserOutput>.Failure("INVALID_SUBJECT", "Subject zorunlu ve 200 karakteri gecmemelidir.");
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200)
        {
            return UseCaseResult<UpsertUserOutput>.Failure("INVALID_DISPLAYNAME", "DisplayName zorunlu ve 200 karakteri gecmemelidir.");
        }

        if (email is not null && email.Length > 200)
        {
            return UseCaseResult<UpsertUserOutput>.Failure("INVALID_EMAIL", "Email 200 karakteri gecemez.");
        }

        var existing = await _identityRepository.GetUserBySubjectWithRolesAsync(subject, cancellationToken);
        if (existing is null)
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Subject = subject,
                DisplayName = displayName,
                Email = email,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _identityRepository.AddUserAsync(user, cancellationToken);
            await _identityRepository.SaveChangesAsync(cancellationToken);

            return UseCaseResult<UpsertUserOutput>.Success(new UpsertUserOutput
            {
                UserId = user.Id,
                IsCreated = true
            });
        }

        existing.DisplayName = displayName;
        existing.Email = email;

        await _identityRepository.SaveChangesAsync(cancellationToken);

        return UseCaseResult<UpsertUserOutput>.Success(new UpsertUserOutput
        {
            UserId = existing.Id,
            IsCreated = false
        });
    }
}

