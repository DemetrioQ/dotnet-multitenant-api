using MediatR;

namespace SaasApi.Application.Features.Admin.Commands.SetTenantStatus;

public record SetTenantStatusCommand(Guid TenantId, bool IsActive) : IRequest;
