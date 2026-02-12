using API.Auth;
using API.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IOptions<OidcOptions> _oidcOptions;

    public AuthController(IOptions<OidcOptions> oidcOptions)
    {
        _oidcOptions = oidcOptions;
    }

    /// <summary>
    /// OIDC login icin authorize URL dondurur.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var options = _oidcOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Authority) || string.IsNullOrWhiteSpace(options.ClientId))
        {
            return BadRequest("OIDC ayarlari eksik.");
        }

        var returnUrl = NormalizeReturnUrl(request.ReturnUrl);
        var encodedReturnUrl = Uri.EscapeDataString(returnUrl);
        var authorizeUrl =
            $"{options.Authority.TrimEnd('/')}/authorize?client_id={Uri.EscapeDataString(options.ClientId)}" +
            $"&response_type=code&scope=openid%20profile&redirect_uri={encodedReturnUrl}";

        return Ok(new LoginResponse
        {
            LoginUrl = authorizeUrl
        });
    }

    private string NormalizeReturnUrl(string? returnUrl)
    {
        // Open redirect riskini azaltmak icin yalnizca local path veya host tabanli fallback kullanir.
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Uri.TryCreate(returnUrl, UriKind.Relative, out var relativeUri) &&
            returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return $"{Request.Scheme}://{Request.Host}{relativeUri}";
        }

        return $"{Request.Scheme}://{Request.Host}/";
    }
}
