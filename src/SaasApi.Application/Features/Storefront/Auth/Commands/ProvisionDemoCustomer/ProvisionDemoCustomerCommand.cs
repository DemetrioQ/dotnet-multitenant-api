using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ProvisionDemoCustomer;

public record ProvisionDemoCustomerCommand : IRequest<ProvisionDemoCustomerResult>;

public record ProvisionDemoCustomerResult(
    string JwtToken,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    DateTime DemoExpiresAt);
