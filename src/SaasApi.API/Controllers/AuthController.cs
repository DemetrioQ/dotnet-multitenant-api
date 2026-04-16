using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Users.Commands.ForgotPassword;
using SaasApi.Application.Features.Users.Commands.ResendVerification;
using SaasApi.Application.Features.Users.Commands.LoginUser;
using SaasApi.Application.Features.Users.Commands.RefreshTokens;
using SaasApi.Application.Features.Users.Commands.RegisterUser;
using SaasApi.Application.Features.Users.Commands.ResetPassword;
using SaasApi.Application.Features.Users.Commands.VerifyEmail;

namespace SaasApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IMediator mediator,
    IWebHostEnvironment env,
    IEmailService emailService,
    IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:5173";
        var verificationLink = $"{frontendUrl}/verify-email?token={result.EmailVerificationToken}";
        await emailService.SendVerificationEmailAsync(command.Email, verificationLink, ct);

        return CreatedAtAction(nameof(Register), new { userId = result.UserId }, new { result.UserId });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        await mediator.Send(new VerifyEmailCommand(token), ct);
        return Ok(new { message = "Email verified successfully." });
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

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        if (result.Token is not null)
        {
            var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:5173";
            var verificationLink = $"{frontendUrl}/verify-email?token={result.Token}";
            await emailService.SendVerificationEmailAsync(command.Email, verificationLink, ct);
        }

        return Ok(new { message = "If the account exists and is unverified, a new verification email has been sent." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        if (result.ResetToken is not null)
        {
            var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:5173";
            var resetLink = $"{frontendUrl}/reset-password?token={result.ResetToken}";
            await emailService.SendPasswordResetEmailAsync(result.Email!, resetLink, ct);
        }

        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(new { message = "Password has been reset successfully." });
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
