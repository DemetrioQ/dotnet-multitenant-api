using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Common.Settings;

namespace SaasApi.Infrastructure.Services;

public class ResendEmailService(
    HttpClient httpClient,
    IOptions<ResendSettings> settings,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private readonly ResendSettings _cfg = settings.Value;

    public Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default) =>
        SendAsync(
            toEmail,
            "Verify your email",
            $"<p>Welcome! Please verify your email by clicking the link below:</p>" +
            $"<p><a href=\"{verificationLink}\">Verify email</a></p>" +
            $"<p>If you didn't create an account, you can ignore this message.</p>",
            ct);

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default) =>
        SendAsync(
            toEmail,
            "Reset your password",
            $"<p>You requested a password reset. Click the link below to choose a new password:</p>" +
            $"<p><a href=\"{resetLink}\">Reset password</a></p>" +
            $"<p>This link expires in 1 hour. If you didn't request this, you can ignore it.</p>",
            ct);

    public Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default) =>
        SendAsync(
            toEmail,
            "You've been invited",
            $"<p>You've been invited to join a team.</p>" +
            $"<p><a href=\"{inviteLink}\">Accept invitation</a></p>",
            ct);

    public Task SendCustomerVerificationEmailAsync(string toEmail, string storeName, string verificationLink, CancellationToken ct = default)
    {
        var safeStore = WebUtility.HtmlEncode(storeName);
        return SendAsync(
            toEmail,
            $"Confirm your email for {storeName}",
            $"<p>Thanks for signing up at <strong>{safeStore}</strong>!</p>" +
            $"<p>Confirm your email address to finish setting up your account:</p>" +
            $"<p><a href=\"{verificationLink}\">Confirm my email</a></p>" +
            $"<p>If you didn't create an account at {safeStore}, you can safely ignore this message.</p>",
            ct);
    }

    public Task SendCustomerPasswordResetEmailAsync(string toEmail, string storeName, string resetLink, CancellationToken ct = default)
    {
        var safeStore = WebUtility.HtmlEncode(storeName);
        return SendAsync(
            toEmail,
            $"Reset your {storeName} password",
            $"<p>We received a request to reset your password for your account at <strong>{safeStore}</strong>.</p>" +
            $"<p><a href=\"{resetLink}\">Choose a new password</a></p>" +
            $"<p>This link expires in 1 hour. If you didn't request a reset, you can ignore this message.</p>",
            ct);
    }

    private async Task SendAsync(string to, string subject, string html, CancellationToken ct)
    {
        var payload = new
        {
            from = $"{_cfg.FromName} <{_cfg.FromAddress}>",
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

        logger.LogInformation("Sent email via Resend. Subject={Subject} To={To}", subject, to);
    }
}
