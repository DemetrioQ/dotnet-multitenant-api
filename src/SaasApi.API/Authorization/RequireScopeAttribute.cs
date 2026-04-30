using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SaasApi.API.Authorization;

/// <summary>
/// Requires the calling principal to have a specific OAuth scope.
/// User tokens (sub_type != "client") bypass scope checks — they go through
/// role-based [Authorize(Roles = ...)] instead. Only OAuth client_credentials
/// tokens are scope-restricted.
///
/// Usage:
/// <code>
/// [Authorize]
/// [RequireScope("products:write")]
/// public IActionResult CreateProduct(...) { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequireScopeAttribute(string scope) : Attribute, IAsyncAuthorizationFilter
{
    public string Scope { get; } = scope;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Anonymous? Let the standard [Authorize] handle it (it produces 401 first).
        if (user.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // User tokens bypass scope checks — role auth covers them.
        var subType = user.FindFirst("sub_type")?.Value;
        if (subType != "client")
            return Task.CompletedTask;

        var scopeClaim = user.FindFirst("scope")?.Value ?? string.Empty;
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
