using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.LoginUser
{
    public class LoginUserHandler(
        IRepository<User> userRepo,
        IRepository<Domain.Entities.RefreshToken> refreshTokenRepo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService
        )
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        public async Task<LoginUserResult> Handle(LoginUserCommand request, CancellationToken ct)
        {
            var existing = await userRepo.FindAsync(u => u.Email == request.Email, ct);
            if (!existing.Any())
                throw new UnauthorizedAccessException("Invalid User");

            var user = existing.First();

            bool isMatch = passwordHasher.Verify(request.Password, user.PasswordHash);

            if (!isMatch)
            {
                throw new UnauthorizedAccessException("Invalid User");
            }

            var refreshToken = Domain.Entities.RefreshToken.Create(user.TenantId, user.Id);
            await refreshTokenRepo.AddAsync(refreshToken);
            await refreshTokenRepo.SaveChangesAsync();

            string jwtToken = jwtTokenService.GenerateToken(user);

            return new LoginUserResult(jwtToken, refreshToken.Token, refreshToken.ExpiresAt);


        }
    }
}
