namespace SaasApi.Application.Common.Interfaces;

public interface IEmailService
{
    // Merchant-side (dashboard users) — generic SaaS copy.
    Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default);

    // Customer-side (storefront shoppers) — branded with the store name.
    Task SendCustomerVerificationEmailAsync(string toEmail, string storeName, string verificationLink, CancellationToken ct = default);
    Task SendCustomerPasswordResetEmailAsync(string toEmail, string storeName, string resetLink, CancellationToken ct = default);
}
