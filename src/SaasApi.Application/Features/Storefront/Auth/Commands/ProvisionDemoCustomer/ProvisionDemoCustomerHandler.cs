using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ProvisionDemoCustomer;

public class ProvisionDemoCustomerHandler(
    IRepository<Customer> customerRepo,
    IRepository<CustomerRefreshToken> refreshRepo,
    ICurrentTenantService currentTenant,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<ProvisionDemoCustomerCommand, ProvisionDemoCustomerResult>
{
    private static readonly TimeSpan CustomerLifetime = TimeSpan.FromDays(7);

    public async Task<ProvisionDemoCustomerResult> Handle(ProvisionDemoCustomerCommand request, CancellationToken ct)
    {
        if (!currentTenant.IsResolved)
            throw new BadRequestException("Demo customer can only be provisioned on a store host.");

        var expiresAt = DateTime.UtcNow.Add(CustomerLifetime);
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"demo-{suffix}@demo.local";

        var passwordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N"));
        var customer = Customer.CreateDemo(
            currentTenant.TenantId, email, passwordHash, "Demo", "Shopper", expiresAt);

        await customerRepo.AddAsync(customer, ct);

        var refresh = CustomerRefreshToken.Create(customer.TenantId, customer.Id, Guid.NewGuid());
        await refreshRepo.AddAsync(refresh, ct);

        await refreshRepo.SaveChangesAsync(ct);

        var jwt = jwtTokenService.GenerateToken(customer);
        return new ProvisionDemoCustomerResult(jwt, refresh.Token, refresh.ExpiresAt, expiresAt);
    }
}
