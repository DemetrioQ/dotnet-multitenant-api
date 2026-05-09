using MediatR;

namespace SaasApi.Application.Features.Demo.Commands.ProvisionDemoTenant;

public record ProvisionDemoTenantCommand : IRequest<ProvisionDemoTenantResult>;

public record ProvisionDemoTenantResult(
    string JwtToken,
    string RefreshToken,
    DateTime RefreshExpiresAt,
    string TenantSlug,
    string TenantName,
    DateTime DemoExpiresAt);
