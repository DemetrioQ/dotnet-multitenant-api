using MediatR;

namespace SaasApi.Application.Features.Users.Commands.RegisterUser;

public record RegisterUserCommand(Guid TenantId, string Email, string Password, string FirstName, string LastName) : IRequest<RegisterUserResult>;

public record RegisterUserResult(Guid UserId, string EmailVerificationToken);
