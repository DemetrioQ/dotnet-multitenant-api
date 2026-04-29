using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.IssueClientToken;

public record IssueClientTokenCommand(string ClientId, string ClientSecret) : IRequest<IssueClientTokenResult>;

public record IssueClientTokenResult(string AccessToken, int ExpiresInSeconds);
