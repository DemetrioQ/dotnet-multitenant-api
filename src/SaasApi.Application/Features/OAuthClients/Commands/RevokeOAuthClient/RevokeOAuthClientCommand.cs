using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.RevokeOAuthClient;

public record RevokeOAuthClientCommand(Guid Id) : IRequest;
