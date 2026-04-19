using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Storefront.Auth.Commands.ForgotCustomerPassword;
using SaasApi.Application.Features.Storefront.Auth.Commands.LoginCustomer;
using SaasApi.Application.Features.Storefront.Auth.Commands.RefreshCustomerTokens;
using SaasApi.Application.Features.Storefront.Auth.Commands.RegisterCustomer;
using SaasApi.Application.Features.Storefront.Auth.Commands.ResendCustomerVerification;
using SaasApi.Application.Features.Storefront.Auth.Commands.ResetCustomerPassword;
using SaasApi.Application.Features.Storefront.Auth.Commands.VerifyCustomerEmail;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/storefront/auth")]
public class StorefrontAuthController(
    IMediator mediator,
    IWebHostEnvironment env,
    IBackgroundJobQueue jobQueue,
    ICurrentTenantService currentTenant) : ControllerBase
{
    private const string CustomerRefreshCookie = "customerRefreshToken";

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        var tenantId = currentTenant.TenantId;
        var storeUrl = result.StoreUrl.TrimEnd('/');
        var verificationLink = $"{storeUrl}/verify-email?token={result.EmailVerificationToken}";
        var model = new CustomerVerificationModel(
            result.StoreName, storeUrl, result.CustomerFirstName, command.Email, verificationLink);

        await jobQueue.EnqueueAsync(async (sp, jobCt) =>
        {
            var emailService = sp.GetRequiredService<IEmailService>();
            await emailService.SendTenantEmailAsync(
                tenantId, result.StoreName, command.Email,
                EmailTemplateType.CustomerVerification, model, jobCt);
        }, ct);

        return CreatedAtAction(nameof(Register), new { customerId = result.CustomerId }, new { result.CustomerId });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        await mediator.Send(new VerifyCustomerEmailCommand(token), ct);
        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Login([FromBody] LoginCustomerCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return Ok(new { jwtToken = result.JwtToken, expiresAt = result.ExpiresAt });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[CustomerRefreshCookie];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized();

        var result = await mediator.Send(new RefreshCustomerTokenCommand(refreshToken), ct);
        SetRefreshTokenCookie(result.RefreshToken, result.ExpiresAt);
        return Ok(new { jwtToken = result.JwtToken, expiresAt = result.ExpiresAt });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CustomerRefreshCookie);
        return NoContent();
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendCustomerVerificationCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        if (result.Token is not null && result.StoreName is not null && result.StoreUrl is not null)
        {
            var tenantId = currentTenant.TenantId;
            var storeUrl = result.StoreUrl.TrimEnd('/');
            var link = $"{storeUrl}/verify-email?token={result.Token}";
            var storeName = result.StoreName;
            var firstName = result.CustomerFirstName ?? "";
            var email = command.Email;
            var model = new CustomerVerificationModel(storeName, storeUrl, firstName, email, link);

            await jobQueue.EnqueueAsync(async (sp, jobCt) =>
            {
                var emailService = sp.GetRequiredService<IEmailService>();
                await emailService.SendTenantEmailAsync(
                    tenantId, storeName, email,
                    EmailTemplateType.CustomerVerification, model, jobCt);
            }, ct);
        }

        return Ok(new { message = "If the account exists and is unverified, a new verification email has been sent." });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotCustomerPasswordCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        if (result.ResetToken is not null && result.StoreName is not null && result.StoreUrl is not null)
        {
            var tenantId = currentTenant.TenantId;
            var storeUrl = result.StoreUrl.TrimEnd('/');
            var link = $"{storeUrl}/reset-password?token={result.ResetToken}";
            var email = result.Email!;
            var storeName = result.StoreName;
            var firstName = result.CustomerFirstName ?? "";
            var model = new CustomerPasswordResetModel(storeName, storeUrl, firstName, email, link);

            await jobQueue.EnqueueAsync(async (sp, jobCt) =>
            {
                var emailService = sp.GetRequiredService<IEmailService>();
                await emailService.SendTenantEmailAsync(
                    tenantId, storeName, email,
                    EmailTemplateType.CustomerPasswordReset, model, jobCt);
            }, ct);
        }

        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetCustomerPasswordCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(new { message = "Password has been reset successfully." });
    }

    private void SetRefreshTokenCookie(string refreshToken, DateTime expiresAt)
    {
        Response.Cookies.Append(CustomerRefreshCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsProduction(),
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt
        });
    }
}
