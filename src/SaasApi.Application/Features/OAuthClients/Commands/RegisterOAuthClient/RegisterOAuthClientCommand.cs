using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.RegisterOAuthClient;

public record RegisterOAuthClientCommand(string Name) : IRequest<RegisterOAuthClientResult>;

public record RegisterOAuthClientResult(string ClientId, string ClientSecret, string Name);
