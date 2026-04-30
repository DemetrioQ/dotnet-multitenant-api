using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Domain.Common;

namespace SaasApi.API.Controllers;

/// <summary>
/// RFC 8414 — OAuth 2.0 Authorization Server Metadata.
/// Discovered by clients (e.g. Claude Code) to bootstrap the auth flow.
/// Served at the root, not under /api/v{version}, per the spec.
/// </summary>
[ApiController]
public class WellKnownController(IConfiguration config) : ControllerBase
{
    [HttpGet("/.well-known/oauth-authorization-server")]
    [AllowAnonymous]
    public IActionResult AuthorizationServerMetadata()
    {
        var issuer = $"{Request.Scheme}://{Request.Host}";
        // The consent UI lives on the dashboard, not the API.
        var dashboardBase = (config["App:FrontendUrl"] ?? issuer).TrimEnd('/');

        return Ok(new
        {
            issuer,
            authorization_endpoint = $"{dashboardBase}/oauth/consent",
            token_endpoint = $"{issuer}/api/v1/oauth/token",
            registration_endpoint = $"{issuer}/api/v1/oauth/register",
            scopes_supported = OAuthScopes.All,
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "client_credentials", "refresh_token" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "none" },
            code_challenge_methods_supported = new[] { "S256" },
        });
    }
}
