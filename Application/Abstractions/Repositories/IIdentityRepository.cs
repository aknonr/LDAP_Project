using Domain.Entities;

namespace Application.Abstractions.Repositories;

/// <summary>
/// AppUser/Role yonetimi icin veri erisim sozlesmesi.
/// </summary>
public interface IIdentityRepository
{
    Task<AppUser?> GetUserByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task<AppUser?> GetUserBySubjectWithRolesAsync(string subject, CancellationToken cancellationToken);
    Task AddUserAsync(AppUser user, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserListItemReadModel>> ListUsersAsync(int skip, int take, CancellationToken cancellationToken);
    Task<int> GetUserCountAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetAllRoleNamesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken);

    /// <summary>
    /// Hedef kullanici disinda aktif bir (Admin veya SuperAdmin) var mi?
    /// Son admin'in silinmesi/rolunun alinmasi ile lockout olmamasi icin kullanilir.
    /// </summary>
    Task<bool> ExistsOtherActiveAdminAsync(Guid excludedUserId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

