namespace SaasApi.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendInvitationEmailAsync(string toEmail, string inviteLink, CancellationToken ct = default);
}
