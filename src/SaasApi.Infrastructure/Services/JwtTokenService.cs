using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Services;

public class JwtTokenService(IConfiguration config, IAppDbContext db) : IJwtTokenService
{
    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("sub_type", "merchant"),
            new(ClaimTypes.Role, user.Role.ToDbString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
        };
        AppendDemoClaimsForTenant(user.TenantId, claims);
        return Build(claims);
    }

    public string GenerateToken(Customer customer)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new("tenant_id", customer.TenantId.ToString()),
            new("sub_type", "customer"),
            new(JwtRegisteredClaimNames.Email, customer.Email),
        };
        if (customer.IsDemo)
        {
            claims.Add(new Claim("demo", "true"));
            if (customer.DemoExpiresAt.HasValue)
                claims.Add(new Claim("demo_expires_at",
                    customer.DemoExpiresAt.Value.ToUniversalTime().ToString("o")));
        }
        return Build(claims);
    }

    private void AppendDemoClaimsForTenant(Guid tenantId, List<Claim> claims)
    {
        // Demo-ness for merchant users derives from their Tenant. Cheap point lookup,
        // bypasses query filters since we may not have tenant context here.
        var tenant = db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.IsDemo, t.DemoExpiresAt })
            .FirstOrDefault();

        if (tenant is null || !tenant.IsDemo) return;
        claims.Add(new Claim("demo", "true"));
        if (tenant.DemoExpiresAt.HasValue)
            claims.Add(new Claim("demo_expires_at",
                tenant.DemoExpiresAt.Value.ToUniversalTime().ToString("o")));
    }

    public string GenerateToken(OAuthClient client)
    {
        // OAuth convention: scopes are space-separated in a single "scope" claim.
        var scopeValue = string.Join(' ', client.GetScopes());
        // client_credentials requires a tenanted client; DCR clients (TenantId=null)
        // are public and only flow through authorization_code, never here.
        if (client.TenantId is null)
            throw new InvalidOperationException("Cannot issue client_credentials token for a tenantless client.");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
            new Claim("tenant_id", client.TenantId.Value.ToString()),
            new Claim("sub_type", "client"),
            new Claim("client_id", client.ClientId),
            new Claim("scope", scopeValue),
            // Keep the admin role so existing [Authorize(Roles = ...)] still passes.
            // Scope checks layer on top via [RequireScope] for finer-grained control.
            new Claim(ClaimTypes.Role, "admin"),
        };
        return Build(claims);
    }

    public string GenerateAuthorizationCodeToken(User user, OAuthClient client, IEnumerable<string> scopes)
    {
        var scopeValue = string.Join(' ', scopes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("tenant_id", user.TenantId.ToString()),
            // sub_type=merchant so audit logs etc. attribute actions to the user, not the client.
            // The client_id claim records which client was the proximate caller.
            new Claim("sub_type", "merchant"),
            new Claim("client_id", client.ClientId),
            new Claim("scope", scopeValue),
            new Claim(ClaimTypes.Role, user.Role.ToDbString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        };
        return Build(claims);
    }

    private string Build(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
