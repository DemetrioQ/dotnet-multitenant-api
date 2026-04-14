using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Users.Commands.LoginUser;
using SaasApi.Application.Features.Users.Commands.RefreshTokens;
using SaasApi.Application.Features.Users.Commands.RegisterUser;

namespace SaasApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Register), new { userId = result.UserId }, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Login), new { token = result.JwtToken, refreshToken = result.RefreshToken, expiresAt = result.ExpiresAt }, result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Login), new { token = result.JwtToken, refreshToken = result.RefreshToken, expiresAt = result.ExpiresAt }, result);
    }
}
