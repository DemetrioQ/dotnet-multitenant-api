using MediatR;

namespace SaasApi.Application.Features.Demo.Commands.ElevateDemoRole;

public record ElevateDemoRoleCommand(string Role) : IRequest<ElevateDemoRoleResult>;

public record ElevateDemoRoleResult(string JwtToken);
