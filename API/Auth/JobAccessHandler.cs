using Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API.Auth;

public sealed class JobAccessHandler : AuthorizationHandler<JobAccessRequirement, JobAccessInfo>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        JobAccessRequirement requirement,
        JobAccessInfo resource)
    {
        if (resource.JobId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var subject = context.User.FindFirstValue("sub")
                      ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(subject)
            && !string.IsNullOrWhiteSpace(resource.RequestedBySubject)
            && string.Equals(resource.RequestedBySubject, subject, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var requestedBy = ResolveRequestedBy(context.User);
        if (!string.IsNullOrWhiteSpace(requestedBy)
            && string.Equals(resource.RequestedBy, requestedBy, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static string ResolveRequestedBy(ClaimsPrincipal user)
    {
        return user.FindFirstValue("preferred_username")
               ?? user.FindFirstValue(ClaimTypes.Name)
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? string.Empty;
    }
}

