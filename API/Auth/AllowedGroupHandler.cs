using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace API.Auth;

public sealed class AllowedGroupHandler : AuthorizationHandler<AllowedGroupRequirement>
{
    private readonly IOptionsMonitor<OidcOptions> _options;

    public AllowedGroupHandler(IOptionsMonitor<OidcOptions> options)
    {
        _options = options;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowedGroupRequirement requirement)
    {
        var settings = _options.CurrentValue;
        if (settings.AllowedGroups is null || settings.AllowedGroups.Count == 0)
        {
            return Task.CompletedTask;
        }

        var claimType = string.IsNullOrWhiteSpace(settings.GroupClaim) ? "groups" : settings.GroupClaim;
        var userGroups = context.User.FindAll(claimType).Select(claim => claim.Value);
        var groupSet = new HashSet<string>(userGroups, StringComparer.OrdinalIgnoreCase);

        foreach (var allowedGroup in settings.AllowedGroups)
        {
            if (groupSet.Contains(allowedGroup))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
