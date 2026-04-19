using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Addresses.Queries.GetMyAddresses;

public class GetMyAddressesHandler(
    IRepository<CustomerAddress> addressRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<GetMyAddressesQuery, IReadOnlyList<CustomerAddressDto>>
{
    public async Task<IReadOnlyList<CustomerAddressDto>> Handle(GetMyAddressesQuery request, CancellationToken ct)
    {
        var all = await addressRepo.FindAsync(a => a.CustomerId == currentCustomer.CustomerId, ct);
        return all
            .OrderByDescending(a => a.IsDefaultShipping || a.IsDefaultBilling)
            .ThenByDescending(a => a.CreatedAt)
            .Select(CustomerAddressDto.FromEntity)
            .ToList();
    }
}
