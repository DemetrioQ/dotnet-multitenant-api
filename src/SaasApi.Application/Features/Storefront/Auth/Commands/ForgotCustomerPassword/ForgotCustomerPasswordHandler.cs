using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ForgotCustomerPassword;

public class ForgotCustomerPasswordHandler(
    IRepository<Customer> customerRepo,
    IRepository<CustomerPasswordResetToken> resetRepo,
    IRepository<Tenant> tenantRepo,
    ICurrentTenantService currentTenantService,
    IStoreUrlBuilder storeUrlBuilder)
    : IRequestHandler<ForgotCustomerPasswordCommand, ForgotCustomerPasswordResult>
{
    public async Task<ForgotCustomerPasswordResult> Handle(ForgotCustomerPasswordCommand request, CancellationToken ct)
    {
        if (!currentTenantService.IsResolved)
            return new ForgotCustomerPasswordResult(null, null, null, null, null);

        var customers = await customerRepo.FindAsync(c => c.Email == request.Email, ct);
        if (!customers.Any() || !customers.First().IsActive)
            return new ForgotCustomerPasswordResult(null, null, null, null, null);

        var customer = customers.First();

        var reset = CustomerPasswordResetToken.Create(customer.TenantId, customer.Id);
        await resetRepo.AddAsync(reset, ct);
        await resetRepo.SaveChangesAsync(ct);

        var tenant = await tenantRepo.GetByIdAsync(customer.TenantId, ct);
        if (tenant is null)
            return new ForgotCustomerPasswordResult(null, null, null, null, null);

        return new ForgotCustomerPasswordResult(
            customer.Email,
            reset.Token,
            tenant.Name,
            storeUrlBuilder.BuildUrl(tenant.Slug),
            customer.FirstName);
    }
}
