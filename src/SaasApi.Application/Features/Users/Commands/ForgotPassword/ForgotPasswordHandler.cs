using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.ForgotPassword;

public class ForgotPasswordHandler(
    IRepository<Tenant> tenantRepo,
    IRepository<User> userRepo,
    IRepository<PasswordResetToken> resetTokenRepo)
    : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var tenants = await tenantRepo.FindGlobalAsync(t => t.Slug == request.Slug, ct);
        if (!tenants.Any())
            return new ForgotPasswordResult(null, null); // anti-enumeration

        var tenant = tenants.First();

        var users = await userRepo.FindGlobalAsync(
            u => u.Email == request.Email && u.TenantId == tenant.Id, ct);

        if (!users.Any() || !users.First().IsActive)
            return new ForgotPasswordResult(null, null); // anti-enumeration

        var user = users.First();

        var resetToken = PasswordResetToken.Create(tenant.Id, user.Id);
        await resetTokenRepo.AddAsync(resetToken, ct);
        await resetTokenRepo.SaveChangesAsync(ct);

        return new ForgotPasswordResult(user.Email, resetToken.Token);
    }
}
