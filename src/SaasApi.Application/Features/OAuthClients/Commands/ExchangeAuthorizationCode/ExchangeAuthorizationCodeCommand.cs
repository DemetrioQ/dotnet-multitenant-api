using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.ExchangeAuthorizationCode;

public record ExchangeAuthorizationCodeCommand(
    string Code,
    string ClientId,
    string RedirectUri,
    string CodeVerifier) : IRequest<ExchangeAuthorizationCodeResult>;

public record ExchangeAuthorizationCodeResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string Scope);
