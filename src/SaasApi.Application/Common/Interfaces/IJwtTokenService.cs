using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    string GenerateToken(Customer customer);
    string GenerateToken(OAuthClient client);

    /// <summary>
    /// JWT for the authorization_code grant — represents the user acting via
    /// a public OAuth client. Carries user identity (sub, role, email) plus
    /// client_id and scope-restricted authorization.
    /// </summary>
    string GenerateAuthorizationCodeToken(User user, OAuthClient client, IEnumerable<string> scopes);
}
