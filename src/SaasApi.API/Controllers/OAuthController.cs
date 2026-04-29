using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.OAuthClients.Commands.IssueClientToken;
using SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;
using SaasApi.Application.Features.OAuthClients.Commands.RevokeOAuthClient;
using SaasApi.Application.Features.OAuthClients.Queries.GetOAuthClients;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/oauth")]
public class OAuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// RFC 6749 §4.4 — Client Credentials Grant.
    /// Accepts application/x-www-form-urlencoded.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token([FromForm] TokenRequestForm form, CancellationToken ct)
    {
        if (!string.Equals(form.GrantType, "client_credentials", StringComparison.Ordinal))
            return BadRequest(new { error = "unsupported_grant_type" });

        if (string.IsNullOrEmpty(form.ClientId) || string.IsNullOrEmpty(form.ClientSecret))
            return BadRequest(new { error = "invalid_request" });

        try
        {
            var result = await mediator.Send(new IssueClientTokenCommand(form.ClientId, form.ClientSecret), ct);
            return Ok(new
            {
                access_token = result.AccessToken,
                token_type = "Bearer",
                expires_in = result.ExpiresInSeconds,
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "invalid_client" });
        }
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
        [FromForm(Name = "grant_type")]
        public string? GrantType { get; set; }

        [FromForm(Name = "client_id")]
        public string? ClientId { get; set; }

        [FromForm(Name = "client_secret")]
        public string? ClientSecret { get; set; }
    }
}
