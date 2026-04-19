using Microsoft.Extensions.Logging;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Services;

/// <summary>
/// Development email service — logs email content instead of sending.
/// Replaced by ResendEmailService when Resend:ApiKey is configured.
/// </summary>
public class EmailService(
    IEmailTemplateRenderer renderer,
    ILogger<EmailService> logger) : IEmailService
{
    public Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] From: SaaS API | To: {Email} | Subject: Verify your email | Link: {Link}",
            toEmail, verificationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] From: SaaS API | To: {Email} | Subject: Reset your password | Link: {Link}",
            toEmail, resetLink);
        return Task.CompletedTask;
    }

    public Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] From: SaaS API | To: {Email} | Subject: You've been invited | Link: {Link}",
            toEmail, inviteLink);
        return Task.CompletedTask;
    }

    public async Task SendTenantEmailAsync(
        Guid tenantId,
        string storeName,
        string toEmail,
        EmailTemplateType type,
        object model,
        CancellationToken ct = default)
    {
        var rendered = await renderer.RenderAsync(tenantId, type, model, ct);
        if (!rendered.Enabled)
        {
            logger.LogInformation(
                "[EMAIL] SKIPPED (disabled) | Tenant: {TenantId} | Type: {Type} | To: {Email}",
                tenantId, type, toEmail);
            return;
        }

        logger.LogInformation(
            "[EMAIL] From: {Store} | To: {Email} | Type: {Type} | Subject: {Subject}",
            storeName, toEmail, type, rendered.Subject);
    }
}
