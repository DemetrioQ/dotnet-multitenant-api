using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Settings;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Services;

public class ResendEmailService(
    HttpClient httpClient,
    IEmailTemplateRenderer renderer,
    IOptions<ResendSettings> settings,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly ResendSettings _cfg = settings.Value;

    public Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default) =>
        SendAsync(
            toEmail,
            fromName: null,
            "Verify your email",
            $"<p>Welcome! Please verify your email by clicking the link below:</p>" +
            $"<p><a href=\"{verificationLink}\">Verify email</a></p>" +
            $"<p>If you didn't create an account, you can ignore this message.</p>",
            ct);

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default) =>
        SendAsync(
            toEmail,
            fromName: null,
            "Reset your password",
            $"<p>You requested a password reset. Click the link below to choose a new password:</p>" +
            $"<p><a href=\"{resetLink}\">Reset password</a></p>" +
            $"<p>This link expires in 1 hour. If you didn't request this, you can ignore it.</p>",
            ct);

    public Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default) =>
        SendAsync(
            toEmail,
            fromName: null,
            "You've been invited",
            $"<p>You've been invited to join a team.</p>" +
            $"<p><a href=\"{inviteLink}\">Accept invitation</a></p>",
            ct);

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
            logger.LogInformation("Tenant email {Type} skipped (disabled) for tenant {TenantId}", type, tenantId);
            return;
        }

        await SendAsync(toEmail, storeName, rendered.Subject, rendered.HtmlBody, ct);
    }

    private async Task SendAsync(string to, string? fromName, string subject, string html, CancellationToken ct)
    {
        var display = SanitizeDisplayName(fromName ?? _cfg.FromName);

        var payload = new
        {
            from = $"{display} <{_cfg.FromAddress}>",
            to = new[] { to },
            subject,
            html
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.ApiKey);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Resend send failed. Status={Status} Body={Body} To={To}", response.StatusCode, body, to);
            response.EnsureSuccessStatusCode();
        }

        logger.LogInformation("Sent email via Resend. From=\"{Display}\" Subject={Subject} To={To}", display, subject, to);
    }

    private static string SanitizeDisplayName(string name) =>
        name.Replace("<", "").Replace(">", "").Replace("\"", "").Trim();
}
