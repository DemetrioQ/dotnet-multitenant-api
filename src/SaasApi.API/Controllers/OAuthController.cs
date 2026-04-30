using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Features.OAuthClients.Commands.ExchangeAuthorizationCode;
using SaasApi.Application.Features.OAuthClients.Commands.IssueAuthorizationCode;
using SaasApi.Application.Features.OAuthClients.Commands.IssueClientToken;
using SaasApi.Application.Features.OAuthClients.Commands.RefreshAccessToken;
using SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;
using SaasApi.Application.Features.OAuthClients.Commands.RevokeOAuthClient;
using SaasApi.Application.Features.OAuthClients.Queries.GetAuthorizeRequestInfo;
using SaasApi.Application.Features.OAuthClients.Queries.GetOAuthClients;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oauth")]
public class OAuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// RFC 6749 token endpoint. Dispatches on grant_type:
    ///   - client_credentials (RFC 6749 §4.4) — confidential clients
    ///   - authorization_code (RFC 6749 §4.1.3) — public clients via PKCE
    ///   - refresh_token (RFC 6749 §6) — rotates on each call
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenRequestForm form, CancellationToken ct)
    {
        try
        {
            switch (form.GrantType)
            {
                case "client_credentials":
                    return await HandleClientCredentials(form, ct);
                case "authorization_code":
                    return await HandleAuthorizationCode(form, ct);
                case "refresh_token":
                    return await HandleRefreshToken(form, ct);
                default:
                    return BadRequest(new { error = "unsupported_grant_type" });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<IActionResult> HandleClientCredentials(TokenRequestForm form, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(form.ClientId) || string.IsNullOrEmpty(form.ClientSecret))
            return BadRequest(new { error = "invalid_request" });

        var result = await mediator.Send(new IssueClientTokenCommand(form.ClientId, form.ClientSecret), ct);
        return Ok(new
        {
            access_token = result.AccessToken,
            token_type = "Bearer",
            expires_in = result.ExpiresInSeconds,
        });
    }

    private async Task<IActionResult> HandleAuthorizationCode(TokenRequestForm form, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(form.Code) ||
            string.IsNullOrEmpty(form.ClientId) ||
            string.IsNullOrEmpty(form.RedirectUri) ||
            string.IsNullOrEmpty(form.CodeVerifier))
        {
            return BadRequest(new { error = "invalid_request" });
        }

        var result = await mediator.Send(new ExchangeAuthorizationCodeCommand(
            form.Code, form.ClientId, form.RedirectUri, form.CodeVerifier), ct);

        return Ok(new
        {
            access_token = result.AccessToken,
            refresh_token = result.RefreshToken,
            token_type = "Bearer",
            expires_in = result.ExpiresInSeconds,
            scope = result.Scope,
        });
    }

    private async Task<IActionResult> HandleRefreshToken(TokenRequestForm form, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(form.RefreshToken) || string.IsNullOrEmpty(form.ClientId))
            return BadRequest(new { error = "invalid_request" });

        var result = await mediator.Send(new RefreshAccessTokenCommand(form.RefreshToken, form.ClientId), ct);
        return Ok(new
        {
            access_token = result.AccessToken,
            refresh_token = result.RefreshToken,
            token_type = "Bearer",
            expires_in = result.ExpiresInSeconds,
            scope = result.Scope,
        });
    }

    /// <summary>
    /// Pre-flight metadata the dashboard's consent screen needs to render
    /// (client name, requested vs grantable scopes). Anonymous — no token required.
    /// </summary>
    [HttpGet("authorize/info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAuthorizeInfo(
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string scope,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAuthorizeRequestInfoQuery(client_id, redirect_uri, scope), ct);
        return Ok(result);
    }

    /// <summary>
    /// User grants consent. Authenticated — the dashboard's consent screen calls
    /// this with the user's bearer token. Returns the redirect URL with the
    /// authorization code; the browser navigates there.
    /// </summary>
    [HttpPost("authorize")]
    [Authorize]
    public async Task<IActionResult> Authorize([FromBody] AuthorizeBody body, CancellationToken ct)
    {
        var result = await mediator.Send(new IssueAuthorizationCodeCommand(
            body.ClientId, body.RedirectUri, body.Scope,
            body.CodeChallenge, body.CodeChallengeMethod, body.State), ct);
        return Ok(new { redirectUrl = result.RedirectUrl });
    }

    [HttpGet("clients")]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> ListClients(CancellationToken ct)
    {
        var result = await mediator.Send(new GetOAuthClientsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("clients")]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> RegisterClient([FromBody] RegisterOAuthClientCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("clients/{id:guid}/revoke")]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> RevokeClient([FromRoute] Guid id, CancellationToken ct)
    {
        await mediator.Send(new RevokeOAuthClientCommand(id), ct);
        return NoContent();
    }

    public class TokenRequestForm
    {
        [FromForm(Name = "grant_type")] public string? GrantType { get; set; }
        [FromForm(Name = "client_id")] public string? ClientId { get; set; }
        [FromForm(Name = "client_secret")] public string? ClientSecret { get; set; }
        [FromForm(Name = "code")] public string? Code { get; set; }
        [FromForm(Name = "redirect_uri")] public string? RedirectUri { get; set; }
        [FromForm(Name = "code_verifier")] public string? CodeVerifier { get; set; }
        [FromForm(Name = "refresh_token")] public string? RefreshToken { get; set; }
    }

    public class AuthorizeBody
    {
        public string ClientId { get; set; } = "";
        public string RedirectUri { get; set; } = "";
        public string Scope { get; set; } = "";
        public string CodeChallenge { get; set; } = "";
        public string CodeChallengeMethod { get; set; } = "";
        public string? State { get; set; }
    }
}
