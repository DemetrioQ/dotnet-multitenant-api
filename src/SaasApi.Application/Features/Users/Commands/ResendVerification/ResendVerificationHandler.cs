using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.ResendVerification;

public class ResendVerificationHandler(
    IRepository<Tenant> tenantRepo,
    IRepository<User> userRepo,
    IRepository<EmailVerificationToken> verificationTokenRepo)
    : IRequestHandler<ResendVerificationCommand, ResendVerificationResult>
{
    private const int CooldownMinutes = 2;

    public async Task<ResendVerificationResult> Handle(ResendVerificationCommand request, CancellationToken ct)
    {
        var tenants = await tenantRepo.FindGlobalAsync(t => t.Slug == request.Slug, ct);
        if (!tenants.Any())
            return new ResendVerificationResult(null); // anti-enumeration

        var tenant = tenants.First();

        var users = await userRepo.FindGlobalAsync(
            u => u.Email == request.Email && u.TenantId == tenant.Id, ct);

        if (!users.Any() || !users.First().IsActive || users.First().IsEmailVerified)
            return new ResendVerificationResult(null); // anti-enumeration / already verified

        var user = users.First();

        var existingTokens = await verificationTokenRepo.FindGlobalAsync(t => t.UserId == user.Id, ct);
        var existingToken = existingTokens.FirstOrDefault();

        if (existingToken is not null)
        {
            var cooldownExpiry = existingToken.CreatedAt.AddMinutes(CooldownMinutes);
            if (cooldownExpiry > DateTime.UtcNow)
                return new ResendVerificationResult(null); // still in cooldown — return silently

            verificationTokenRepo.Remove(existingToken);
        }

        var newToken = EmailVerificationToken.Create(user.TenantId, user.Id);
        await verificationTokenRepo.AddAsync(newToken, ct);
        await verificationTokenRepo.SaveChangesAsync(ct);

        return new ResendVerificationResult(newToken.Token);
    }
}
