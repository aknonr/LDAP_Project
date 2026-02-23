using Microsoft.AspNetCore.Authorization;

namespace API.Auth;

/// <summary>
/// Kullanici DB allowlist'te (AppUsers) var ve aktif olmalidir.
/// </summary>
public sealed class DbUserAllowlistRequirement : IAuthorizationRequirement
{
}

