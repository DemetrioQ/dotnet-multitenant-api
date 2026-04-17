using Microsoft.Extensions.Logging;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services;

/// <summary>
/// Development email service — logs email content instead of sending.
/// Replace with a real SMTP/SendGrid implementation when background jobs are added.
/// </summary>
public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {Email} | Subject: Verify your email | Link: {Link}",
            toEmail, verificationLink);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {Email} | Subject: Reset your password | Link: {Link}",
            toEmail, resetLink);

        return Task.CompletedTask;
    }

    public Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {Email} | Subject: You've been invited | Link: {Link}",
            toEmail, inviteLink);

        return Task.CompletedTask;
    }
}
