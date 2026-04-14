using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserHandler(
    IRepository<User> userRepo,
    ICurrentTenantService tenantService,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtService)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await userRepo.FindAsync(u => u.Email == request.Email, ct);
        if (existing.Any())
            throw new InvalidOperationException("A user with this email already exists in this tenant.");

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(tenantService.TenantId, request.Email, passwordHash);
        await userRepo.AddAsync(user, ct);
        await userRepo.SaveChangesAsync(ct);

        var token = jwtService.GenerateToken(user);
        return new RegisterUserResult(user.Id, token);
    }
}
