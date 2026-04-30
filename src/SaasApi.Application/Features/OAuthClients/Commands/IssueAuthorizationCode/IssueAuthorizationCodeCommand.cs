using MediatR;

namespace SaasApi.Application.Features.OAuthClients.Commands.IssueAuthorizationCode;

public record IssueAuthorizationCodeCommand(
    string ClientId,
    string RedirectUri,
    string Scope,
    string CodeChallenge,
    string CodeChallengeMethod,
    string? State) : IRequest<IssueAuthorizationCodeResult>;

public record IssueAuthorizationCodeResult(string Code, string RedirectUrl);
