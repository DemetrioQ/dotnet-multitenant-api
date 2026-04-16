using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserHandler(
    IRepository<User> userRepo,
    IRepository<RefreshToken> refreshTokenRepo,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtService)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await userRepo.FindGlobalAsync(
            u => u.Email == request.Email && u.TenantId == request.TenantId, ct);
        if (existing.Any())
            throw new ConflictException("A user with this email already exists in this tenant.");

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(request.TenantId, request.Email, passwordHash);
        await userRepo.AddAsync(user, ct);
        await userRepo.SaveChangesAsync(ct);

        var token = jwtService.GenerateToken(user);

        var refreshToken = RefreshToken.Create(request.TenantId, user.Id);
        await refreshTokenRepo.AddAsync(refreshToken, ct);
        await refreshTokenRepo.SaveChangesAsync(ct);

        return new RegisterUserResult(user.Id, token, refreshToken.Token, refreshToken.ExpiresAt);
    }
}
