namespace Application.Services.Rbac;

/// <summary>
/// Uygulama icindeki sabit rol adlari.
/// </summary>
public static class KnownRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Admin,
        Operator,
        Viewer,
        SuperAdmin
    };

    public static bool TryNormalize(string roleName, out string normalized)
    {
        if (string.Equals(roleName, Admin, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Admin;
            return true;
        }

        if (string.Equals(roleName, Operator, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Operator;
            return true;
        }

        if (string.Equals(roleName, Viewer, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Viewer;
            return true;
        }

        if (string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            normalized = SuperAdmin;
            return true;
        }

        normalized = string.Empty;
        return false;
    }
}

