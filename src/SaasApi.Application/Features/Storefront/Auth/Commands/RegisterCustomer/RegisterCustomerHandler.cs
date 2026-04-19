using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.RegisterCustomer;

public class RegisterCustomerHandler(
    IRepository<Customer> customerRepo,
    IRepository<CustomerEmailVerificationToken> verificationRepo,
    IRepository<Tenant> tenantRepo,
    ICurrentTenantService currentTenantService,
    IStoreUrlBuilder storeUrlBuilder,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCustomerCommand, RegisterCustomerResult>
{
    public async Task<RegisterCustomerResult> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        if (!currentTenantService.IsResolved)
            throw new NotFoundException("Store not found.");

        var tenantId = currentTenantService.TenantId;
        var tenant = await tenantRepo.GetByIdAsync(tenantId, ct)
                     ?? throw new NotFoundException("Store not found.");

        var existing = await customerRepo.FindAsync(c => c.Email == request.Email, ct);
        if (existing.Any())
            throw new ConflictException("A customer with this email already exists in this store.");

        var passwordHash = passwordHasher.Hash(request.Password);
        var customer = Customer.Create(tenantId, request.Email, passwordHash, request.FirstName, request.LastName);
        await customerRepo.AddAsync(customer, ct);
        await customerRepo.SaveChangesAsync(ct);

        var verification = CustomerEmailVerificationToken.Create(tenantId, customer.Id);
        await verificationRepo.AddAsync(verification, ct);
        await verificationRepo.SaveChangesAsync(ct);

        return new RegisterCustomerResult(
            customer.Id,
            verification.Token,
            tenant.Name,
            storeUrlBuilder.BuildUrl(tenant.Slug),
            customer.FirstName);
    }
}
