using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.RefreshCustomerTokens;

public class RefreshCustomerTokenHandler(
    IRepository<CustomerRefreshToken> refreshRepo,
    IRepository<Customer> customerRepo,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RefreshCustomerTokenCommand, RefreshCustomerTokenResult>
{
    public async Task<RefreshCustomerTokenResult> Handle(RefreshCustomerTokenCommand request, CancellationToken ct)
    {
        var existing = await refreshRepo.FindGlobalAsync(r => r.Token == request.RefreshToken, ct);
        if (!existing.Any())
            throw new UnauthorizedAccessException("Invalid RefreshToken");

        var token = existing.First();

        // Reuse detection — if already revoked, wipe the entire family.
        if (token.RevokedAt != null)
        {
            var family = await refreshRepo.FindGlobalAsync(r => r.FamilyId == token.FamilyId, ct);
            foreach (var t in family.Where(t => t.RevokedAt == null))
                t.Revoke();
            await refreshRepo.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid RefreshToken");
        }

        if (token.IsExpired)
            throw new UnauthorizedAccessException("Invalid RefreshToken");

        token.Revoke();

        var newToken = CustomerRefreshToken.Create(token.TenantId, token.CustomerId, token.FamilyId);
        await refreshRepo.AddAsync(newToken, ct);
        await refreshRepo.SaveChangesAsync(ct);

        var customers = await customerRepo.FindGlobalAsync(c => c.Id == token.CustomerId, ct);
        var customer = customers.FirstOrDefault();
        if (customer is null || !customer.IsActive)
            throw new UnauthorizedAccessException("Invalid RefreshToken");

        var jwt = jwtTokenService.GenerateToken(customer);
        return new RefreshCustomerTokenResult(jwt, newToken.Token, newToken.ExpiresAt);
    }
}
