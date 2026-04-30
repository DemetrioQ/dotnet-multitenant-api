using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.RefreshAccessToken;

public record RefreshAccessTokenCommand(
    string RefreshToken,
    string ClientId) : IRequest<RefreshAccessTokenResult>;

public record RefreshAccessTokenResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string Scope);
