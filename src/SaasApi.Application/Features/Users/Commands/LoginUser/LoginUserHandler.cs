using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.LoginUser
{
    public class LoginUserHandler(
        IRepository<User> userRepo,
        IRepository<Tenant> tenantRepo,
        IRepository<Domain.Entities.RefreshToken> refreshTokenRepo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService
        )
        : IRequestHandler<LoginUserCommand, LoginUserResult>
    {
        public async Task<LoginUserResult> Handle(LoginUserCommand request, CancellationToken ct)
        {
            var tenants = await tenantRepo.FindGlobalAsync(t => t.Slug == request.Slug, ct);
            if (!tenants.Any())
                throw new UnauthorizedAccessException("Invalid credentials");

            var tenant = tenants.First();

            var existing = await userRepo.FindGlobalAsync(
                u => u.Email == request.Email && u.TenantId == tenant.Id, ct);
            if (!existing.Any())
                throw new UnauthorizedAccessException("Invalid credentials");

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
