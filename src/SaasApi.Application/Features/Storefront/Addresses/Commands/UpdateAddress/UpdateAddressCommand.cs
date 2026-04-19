using MediatR;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.UpdateAddress;

public record UpdateAddressCommand(
    Guid Id,
    string? Label,
    AddressInput Address,
    bool IsDefaultShipping,
    bool IsDefaultBilling) : IRequest<CustomerAddressDto>;
