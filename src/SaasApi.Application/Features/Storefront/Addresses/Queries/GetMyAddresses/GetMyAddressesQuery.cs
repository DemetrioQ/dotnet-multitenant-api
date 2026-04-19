using MediatR;

namespace SaasApi.Application.Features.Storefront.Addresses.Queries.GetMyAddresses;

public record GetMyAddressesQuery : IRequest<IReadOnlyList<CustomerAddressDto>>;
