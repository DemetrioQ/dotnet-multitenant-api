using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.UpdateAddress;

public class UpdateAddressHandler(
    IRepository<CustomerAddress> addressRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<UpdateAddressCommand, CustomerAddressDto>
{
    public async Task<CustomerAddressDto> Handle(UpdateAddressCommand request, CancellationToken ct)
    {
        var matches = await addressRepo.FindAsync(
            a => a.Id == request.Id && a.CustomerId == currentCustomer.CustomerId, ct);
        var address = matches.FirstOrDefault()
                      ?? throw new NotFoundException("Address not found.");

        address.Update(request.Label, new Address(
            request.Address.Line1, request.Address.Line2,
            request.Address.City, request.Address.Region,
            request.Address.PostalCode, request.Address.Country));

        // Single-default rule: when flipping a flag on, unset every other of the same kind.
        if (request.IsDefaultShipping && !address.IsDefaultShipping)
        {
            var others = await addressRepo.FindAsync(
                a => a.CustomerId == currentCustomer.CustomerId
                     && a.IsDefaultShipping
                     && a.Id != request.Id, ct);
            foreach (var o in others)
            {
                o.SetDefaultShipping(false);
                addressRepo.Update(o);
            }
        }
        if (request.IsDefaultBilling && !address.IsDefaultBilling)
        {
            var others = await addressRepo.FindAsync(
                a => a.CustomerId == currentCustomer.CustomerId
                     && a.IsDefaultBilling
                     && a.Id != request.Id, ct);
            foreach (var o in others)
            {
                o.SetDefaultBilling(false);
                addressRepo.Update(o);
            }
        }

        address.SetDefaultShipping(request.IsDefaultShipping);
        address.SetDefaultBilling(request.IsDefaultBilling);

        addressRepo.Update(address);
        await addressRepo.SaveChangesAsync(ct);

        return CustomerAddressDto.FromEntity(address);
    }
}
