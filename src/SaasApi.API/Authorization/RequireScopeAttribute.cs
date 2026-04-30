using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SaasApi.API.Authorization;

/// <summary>
/// Requires the calling principal to hold a specific OAuth scope.
///
/// Tokens issued without a scope claim (legacy user logins) bypass this
/// check — role-based [Authorize(Roles = ...)] is the gate for them.
/// Tokens with a scope claim — both client_credentials and authorization_code
/// grants — are scope-restricted regardless of sub_type.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequireScopeAttribute(string scope) : Attribute, IAsyncAuthorizationFilter
{
    public string Scope { get; } = scope;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Anonymous? Standard [Authorize] handles that with a 401 first.
        if (user.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var scopeClaim = user.FindFirst("scope")?.Value;

        // No scope claim → not an OAuth-restricted token. Role auth is the gate.
        if (string.IsNullOrEmpty(scopeClaim))
            return Task.CompletedTask;

        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!scopes.Contains(Scope, StringComparer.Ordinal))
        {
            context.Result = new ObjectResult(new { error = "insufficient_scope", required = Scope })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }

        return Task.CompletedTask;
    }
}
