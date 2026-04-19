using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.CreateAddress;

public class CreateAddressHandler(
    IRepository<CustomerAddress> addressRepo,
    ICurrentTenantService currentTenant,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<CreateAddressCommand, CustomerAddressDto>
{
    public async Task<CustomerAddressDto> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        var customerAddresses = await addressRepo.FindAsync(a => a.CustomerId == currentCustomer.CustomerId, ct);
        var isFirstAddress = customerAddresses.Count == 0;

        // Single-default rule: if this one is being set default, unset any other current default.
        if (request.IsDefaultShipping || isFirstAddress)
        {
            foreach (var a in customerAddresses.Where(a => a.IsDefaultShipping))
            {
                a.SetDefaultShipping(false);
                addressRepo.Update(a);
            }
        }
        if (request.IsDefaultBilling || isFirstAddress)
        {
            foreach (var a in customerAddresses.Where(a => a.IsDefaultBilling))
            {
                a.SetDefaultBilling(false);
                addressRepo.Update(a);
            }
        }

        // First address a customer adds becomes their default for both by convention.
        var defaultShipping = request.IsDefaultShipping || isFirstAddress;
        var defaultBilling = request.IsDefaultBilling || isFirstAddress;

        var entity = CustomerAddress.Create(
            currentTenant.TenantId,
            currentCustomer.CustomerId,
            request.Label,
            new Address(
                request.Address.Line1, request.Address.Line2,
                request.Address.City, request.Address.Region,
                request.Address.PostalCode, request.Address.Country),
            defaultShipping,
            defaultBilling);

        await addressRepo.AddAsync(entity, ct);
        await addressRepo.SaveChangesAsync(ct);

        return CustomerAddressDto.FromEntity(entity);
    }
}
