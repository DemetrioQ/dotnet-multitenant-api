using MediatR;

namespace SaasApi.Application.Features.Users.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Slug, string Email) : IRequest<ForgotPasswordResult>;

public record ForgotPasswordResult(string? Email, string? ResetToken);
