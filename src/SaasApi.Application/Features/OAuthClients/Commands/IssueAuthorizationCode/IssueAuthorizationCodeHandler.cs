using MediatR;
using SaasApi.Application.Common.Auth;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.OAuthClients.Commands.IssueAuthorizationCode;

public class IssueAuthorizationCodeHandler(
    IRepository<OAuthClient> clientRepo,
    IRepository<OAuthAuthorizationCode> codeRepo,
    ICurrentTenantService tenantService,
    ICurrentUserService currentUser)
    : IRequestHandler<IssueAuthorizationCodeCommand, IssueAuthorizationCodeResult>
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);

    public async Task<IssueAuthorizationCodeResult> Handle(IssueAuthorizationCodeCommand request, CancellationToken ct)
    {
        if (!tenantService.IsResolved)
            throw new UnauthorizedAccessException("Tenant context required.");

        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated user required.");

        // Cross-tenant lookup: DCR clients (RFC 7591) have TenantId=null and any
        // user can authorize them. Tenanted clients (registered via the dashboard)
        // can only be authorized by users of the same tenant.
        var clients = await clientRepo.FindGlobalAsync(c => c.ClientId == request.ClientId && !c.IsRevoked, ct);
        var client = clients.FirstOrDefault()
            ?? throw new NotFoundException("invalid_client");

        if (client.TenantId is not null && client.TenantId != tenantService.TenantId)
            throw new NotFoundException("invalid_client");

        if (client.ClientType != OAuthClientType.Public)
            throw new BadRequestException("Authorization code flow requires a public client.");

        if (!client.IsRedirectUriAllowed(request.RedirectUri))
            throw new BadRequestException("invalid_redirect_uri");

        if (!string.Equals(request.CodeChallengeMethod, PkceVerifier.S256, StringComparison.Ordinal))
            throw new BadRequestException("Only S256 code_challenge_method is supported.");

        // Intersect requested scopes with what the client is registered for.
        var requested = (request.Scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var granted = requested
            .Where(s => client.GetScopes().Contains(s, StringComparer.Ordinal))
            .ToList();
        if (granted.Count == 0)
            throw new BadRequestException("invalid_scope");

        var code = SecureRandom.UrlSafeToken(32);
        var entity = OAuthAuthorizationCode.Issue(
            tenantId: tenantService.TenantId,
            code: code,
            oauthClientId: client.Id,
            userId: userId,
            redirectUri: request.RedirectUri,
            codeChallenge: request.CodeChallenge,
            codeChallengeMethod: request.CodeChallengeMethod,
            scopes: granted,
            lifetime: CodeLifetime);

        await codeRepo.AddAsync(entity, ct);
        await codeRepo.SaveChangesAsync(ct);

        var redirectUrl = AssembleRedirect(request.RedirectUri, code, request.State);
        return new IssueAuthorizationCodeResult(code, redirectUrl);
    }

    private static string AssembleRedirect(string redirectUri, string code, string? state)
    {
        var separator = redirectUri.Contains('?') ? '&' : '?';
        var sb = new System.Text.StringBuilder(redirectUri);
        sb.Append(separator).Append("code=").Append(Uri.EscapeDataString(code));
        if (!string.IsNullOrEmpty(state))
            sb.Append("&state=").Append(Uri.EscapeDataString(state));
        return sb.ToString();
    }
}
