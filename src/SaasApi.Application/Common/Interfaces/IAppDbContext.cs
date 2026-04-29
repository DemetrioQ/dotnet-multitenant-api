using Microsoft.EntityFrameworkCore;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common.Interfaces;

/// <summary>
/// Exposes EF's DbSets so handlers can compose SQL-translated queries
/// (Where + GroupBy + Sum + pagination) instead of loading everything into
/// memory via IRepository&lt;T&gt;.FindAsync.
///
/// Clean-architecture note: this leaks EF Core's DbSet/IQueryable out of
/// Infrastructure. Accepted tradeoff for performance-critical read paths —
/// IRepository&lt;T&gt; is still the default for simple write flows.
/// </summary>
public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<TenantOnboardingStatus> TenantOnboardingStatuses { get; }
    DbSet<AuditLogEntry> AuditLogEntries { get; }
    DbSet<TenantSettings> TenantSettings { get; }
    DbSet<OAuthClient> OAuthClients { get; }
}
