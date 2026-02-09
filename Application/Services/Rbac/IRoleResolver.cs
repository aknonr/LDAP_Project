namespace Application.Services.Rbac;

public interface IRoleResolver
{
    Task<IReadOnlyCollection<string>> GetRolesForSubjectAsync(string subject, CancellationToken cancellationToken);
}
