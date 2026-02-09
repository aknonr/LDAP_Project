using Application.Services.Rbac;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace API.Auth;

public sealed class RbacClaimsTransformation : IClaimsTransformation
{
    private readonly IRoleResolver _roleResolver;

    public RbacClaimsTransformation(IRoleResolver roleResolver)
    {
        _roleResolver = roleResolver;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        var subject = principal.FindFirstValue("sub")
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            return principal;
        }

        var roles = await _roleResolver.GetRolesForSubjectAsync(subject, CancellationToken.None);
        foreach (var role in roles)
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return principal;
    }
}
