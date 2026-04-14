using MediatR;

namespace SaasApi.Application.Features.Users.Commands.LoginUser
{
    public record LoginUserCommand(string Email, string Password) : IRequest<LoginUserResult>;

    public record LoginUserResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
}
