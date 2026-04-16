using MediatR;

namespace SaasApi.Application.Features.Users.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
