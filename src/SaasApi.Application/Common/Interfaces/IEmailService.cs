using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common.Interfaces;

public interface IEmailService
{
    // Merchant-side (dashboard users) — generic SaaS copy, not tenant-customizable.
    Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default);

    /// <summary>
    /// Sends an email whose subject/body are defined by the tenant's
    /// <see cref="TenantEmailTemplate"/> (or the default if no override exists).
    /// The store name is used as the sender display name in production.
    /// Returns silently without sending when the resolved template is disabled.
    /// </summary>
    Task SendTenantEmailAsync(
        Guid tenantId,
        string storeName,
        string toEmail,
        EmailTemplateType type,
        object model,
        CancellationToken ct = default);
}
