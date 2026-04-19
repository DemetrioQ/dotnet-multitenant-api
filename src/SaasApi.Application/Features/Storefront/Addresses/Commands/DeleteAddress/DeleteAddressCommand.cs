using MediatR;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.DeleteAddress;

public record DeleteAddressCommand(Guid Id) : IRequest;
