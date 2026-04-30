namespace SaasApi.Domain.Common;

/// <summary>
/// Catalog of OAuth scopes a client_credentials client can be granted.
/// User tokens (sub_type=merchant) bypass scope checks — role auth covers them.
/// Only client tokens (sub_type=client) are scope-restricted.
/// </summary>
public static class OAuthScopes
{
    public const string ProductsRead = "products:read";
    public const string ProductsWrite = "products:write";
    public const string OrdersRead = "orders:read";
    public const string OrdersWrite = "orders:write";
    public const string CustomersRead = "customers:read";
    public const string DashboardRead = "dashboard:read";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ProductsRead,
        ProductsWrite,
        OrdersRead,
        OrdersWrite,
        CustomersRead,
        DashboardRead,
    };

    public static bool IsValid(string scope) => All.Contains(scope);
}
