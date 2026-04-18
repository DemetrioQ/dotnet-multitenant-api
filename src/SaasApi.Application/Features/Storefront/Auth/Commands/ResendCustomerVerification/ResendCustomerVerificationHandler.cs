using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ResendCustomerVerification;

public class ResendCustomerVerificationHandler(
    IRepository<Customer> customerRepo,
    IRepository<CustomerEmailVerificationToken> verificationRepo,
    IRepository<Tenant> tenantRepo,
    ICurrentTenantService currentTenantService)
    : IRequestHandler<ResendCustomerVerificationCommand, ResendCustomerVerificationResult>
{
    private const int CooldownMinutes = 2;

    public async Task<ResendCustomerVerificationResult> Handle(ResendCustomerVerificationCommand request, CancellationToken ct)
    {
        if (!currentTenantService.IsResolved)
            return new ResendCustomerVerificationResult(null, null);

        var customers = await customerRepo.FindAsync(c => c.Email == request.Email, ct);
        if (!customers.Any() || !customers.First().IsActive || customers.First().IsEmailVerified)
            return new ResendCustomerVerificationResult(null, null);

        var customer = customers.First();

        var existingTokens = await verificationRepo.FindAsync(t => t.CustomerId == customer.Id, ct);
        var existing = existingTokens.FirstOrDefault();

        if (existing is not null)
        {
            var cooldownExpiry = existing.CreatedAt.AddMinutes(CooldownMinutes);
            if (cooldownExpiry > DateTime.UtcNow)
                return new ResendCustomerVerificationResult(null, null);

            verificationRepo.Remove(existing);
        }

        var newToken = CustomerEmailVerificationToken.Create(customer.TenantId, customer.Id);
        await verificationRepo.AddAsync(newToken, ct);
        await verificationRepo.SaveChangesAsync(ct);

        var tenant = await tenantRepo.GetByIdAsync(customer.TenantId, ct);
        return new ResendCustomerVerificationResult(newToken.Token, tenant?.Name);
    }
}
