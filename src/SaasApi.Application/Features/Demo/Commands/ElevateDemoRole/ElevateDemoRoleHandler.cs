using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Demo.Commands.ElevateDemoRole;

public class ElevateDemoRoleHandler(
    IAppDbContext db,
    IRepository<User> userRepo,
    ICurrentUserService currentUser,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<ElevateDemoRoleCommand, ElevateDemoRoleResult>
{
    public async Task<ElevateDemoRoleResult> Handle(ElevateDemoRoleCommand request, CancellationToken ct)
    {
        var users = await userRepo.FindGlobalAsync(u => u.Id == currentUser.UserId, ct);
        var user = users.FirstOrDefault()
            ?? throw new UnauthorizedAccessException("Authenticated user not found.");

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, ct)
            ?? throw new NotFoundException($"Tenant {user.TenantId} not found.");

        // Only demo subjects can flip their own role. Real users require an admin
        // workflow (which this handler intentionally does not provide).
        if (!tenant.IsDemo)
            throw new ForbiddenException("Role elevation is only available on demo accounts.");

        user.UpdateRole(UserRoleExtensions.ParseRole(request.Role));
        await userRepo.SaveChangesAsync(ct);

        var jwt = jwtTokenService.GenerateToken(user);
        return new ElevateDemoRoleResult(jwt);
    }
}
