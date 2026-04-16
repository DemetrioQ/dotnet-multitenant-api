namespace SaasApi.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string verificationLink, CancellationToken ct = default);
}
