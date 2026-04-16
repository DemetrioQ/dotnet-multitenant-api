using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Users.Commands.LoginUser;
using SaasApi.Application.Features.Users.Commands.RefreshTokens;
using SaasApi.Application.Features.Users.Commands.RegisterUser;

namespace SaasApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return CreatedAtAction(nameof(Register), new { userId = result.UserId },
           new { result.UserId, jwtToken = result.Token, expiresAt = result.ExpiresAt });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return Ok(new { jwtToken = result.JwtToken, expiresAt = result.ExpiresAt });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await mediator.Send(new RefreshTokenCommand(refreshToken), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return Ok(new { jwtToken = result.JwtToken, expiresAt = result.ExpiresAt });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsProduction(),
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });
    }
}
