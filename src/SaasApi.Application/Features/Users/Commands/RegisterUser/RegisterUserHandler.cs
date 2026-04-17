using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserHandler(
    IRepository<User> userRepo,
    IRepository<EmailVerificationToken> verificationTokenRepo,
    IRepository<UserProfile> profileRepo,
    IRepository<TenantOnboardingStatus> onboardingRepo,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await userRepo.FindGlobalAsync(
            u => u.Email == request.Email && u.TenantId == request.TenantId, ct);
        if (existing.Any())
            throw new ConflictException("A user with this email already exists in this tenant.");

        var tenantUsers = await userRepo.FindGlobalAsync(u => u.TenantId == request.TenantId, ct);
        var role = tenantUsers.Any() ? "member" : "admin";

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.TenantId, request.Email, passwordHash, role);
        await userRepo.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id, user.TenantId, request.FirstName, request.LastName);
        await profileRepo.AddAsync(profile, ct);

        // Flip onboarding ProfileCompleted since firstName is provided
        var statuses = await onboardingRepo.FindGlobalAsync(s => s.TenantId == request.TenantId, ct);
        var status = statuses.FirstOrDefault();
        if (status is not null && !status.ProfileCompleted)
            status.CompleteProfile();

        await userRepo.SaveChangesAsync(ct);

        var verificationToken = EmailVerificationToken.Create(user.TenantId, user.Id);
        await verificationTokenRepo.AddAsync(verificationToken, ct);
        await verificationTokenRepo.SaveChangesAsync(ct);

        return new RegisterUserResult(user.Id, verificationToken.Token);
    }
}
