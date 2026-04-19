using MediatR;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.CreateAddress;

public record CreateAddressCommand(
    string? Label,
    AddressInput Address,
    bool IsDefaultShipping = false,
    bool IsDefaultBilling = false) : IRequest<CustomerAddressDto>;
