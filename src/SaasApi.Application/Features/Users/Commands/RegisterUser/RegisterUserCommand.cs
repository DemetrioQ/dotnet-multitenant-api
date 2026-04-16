using MediatR;

namespace SaasApi.Application.Features.Users.Commands.RegisterUser;

public record RegisterUserCommand(Guid TenantId, string Email, string Password) : IRequest<RegisterUserResult>;

public record RegisterUserResult(Guid UserId, string EmailVerificationToken);
