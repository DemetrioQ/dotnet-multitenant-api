using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserHandler(
    IRepository<User> userRepo,
    IRepository<EmailVerificationToken> verificationTokenRepo,
    IPasswordHasher passwordHasher)
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

        var verificationToken = EmailVerificationToken.Create(user.TenantId, user.Id);
        await verificationTokenRepo.AddAsync(verificationToken, ct);
        await verificationTokenRepo.SaveChangesAsync(ct);

        return new RegisterUserResult(user.Id, verificationToken.Token);
    }
}
