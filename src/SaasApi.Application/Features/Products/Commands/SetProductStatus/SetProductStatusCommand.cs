using MediatR;

namespace SaasApi.Application.Features.Products.Commands.SetProductStatus;

public record SetProductStatusCommand(Guid Id, bool IsActive) : IRequest;
