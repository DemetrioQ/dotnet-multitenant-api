using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.RefreshTokens
{
    public class RefreshTokenHandler(
        IRepository<RefreshToken> refreshTokenRepo,
        IRepository<User> userRepo,
        IJwtTokenService jwtTokenService
        ) : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
    {
        public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            var existing = await refreshTokenRepo.FindGlobalAsync(r => r.Token == request.RefreshToken, ct);
            if (!existing.Any())
                throw new UnauthorizedAccessException("Invalid RefreshToken");

            var refreshToken = existing.First();

            if (!refreshToken.IsValid)
                throw new UnauthorizedAccessException("Invalid RefreshToken");

            refreshToken.Revoke();

            var newRefreshToken = RefreshToken.Create(refreshToken.TenantId, refreshToken.UserId);
            await refreshTokenRepo.AddAsync(newRefreshToken);
            await refreshTokenRepo.SaveChangesAsync();

            var users = await userRepo.FindGlobalAsync(u => u.Id == refreshToken.UserId, ct);
            var user = users.FirstOrDefault();

            if(user is null) 
                throw new UnauthorizedAccessException("Invalid RefreshToken");



            var jwtToken = jwtTokenService.GenerateToken(user);

            return new RefreshTokenResult(jwtToken, newRefreshToken.Token, newRefreshToken.ExpiresAt);

        }
    }
}
