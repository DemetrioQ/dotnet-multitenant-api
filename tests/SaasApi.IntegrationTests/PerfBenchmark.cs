using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Domain.Entities;
using SaasApi.Infrastructure.Persistence;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit.Abstractions;

namespace SaasApi.IntegrationTests;

/// <summary>
/// Scaling benchmark of the optimized read endpoints. A [Theory] row per tier
/// (low / medium / high) seeds that tier's data and measures p50/p95/p99 for the
/// dashboard, orders list, deep-paginated orders, and storefront products.
///
/// Tenant 0 is the "big" tenant with the bulk of the data; other tenants exist
/// only to put pressure on the global tenant filter. All measurements target
/// tenant 0.
///
/// SQLite in-memory is much faster than SQL Server + real network, so absolute
/// latencies are toy numbers. The useful signals are:
///   1. How p95 scales with data volume (flat vs linear).
///   2. Where query-planner cliffs show up (OFFSET pagination at 1M rows, etc.).
/// </summary>
[Collection("Integration")]
[Trait("Category", "Benchmark")]
public class PerfBenchmark(WebAppFactory factory, ITestOutputHelper output) : IntegrationTestBase(factory)
{
    private const int RequestsPerEndpoint = 30;

    private record TenantResult(Guid TenantId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);

    private record SeedPlan(
        string Tier,
        int TenantCount,
        int PrimaryProducts,
        int PrimaryCustomers,
        int PrimaryOrders,
        int OtherProducts,
        int OtherCustomers,
        int OtherOrders);

    [Theory]
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    public async Task Measure_Scaling(string tier)
    {
        var plan = tier switch
        {
            // 1 tenant, tiny dataset — matches the original benchmark.
            "low"    => new SeedPlan(tier, TenantCount:  1, PrimaryProducts:  300, PrimaryCustomers:   100, PrimaryOrders:     2_000, OtherProducts:   0, OtherCustomers:  0, OtherOrders:   0),
            // 10 tenants. Primary has hundreds of thousands of orders.
            "medium" => new SeedPlan(tier, TenantCount: 10, PrimaryProducts: 1_000, PrimaryCustomers: 2_000, PrimaryOrders:   200_000, OtherProducts: 100, OtherCustomers: 50, OtherOrders: 100),
            // 50 tenants. Primary has 1M orders → ~2M order items.
            "high"   => new SeedPlan(tier, TenantCount: 50, PrimaryProducts: 5_000, PrimaryCustomers: 10_000, PrimaryOrders: 1_000_000, OtherProducts: 100, OtherCustomers: 50, OtherOrders: 200),
            _ => throw new ArgumentException($"Unknown tier '{tier}'", nameof(tier))
        };

        // ── Seed ───────────────────────────────────────────────────────────
        var seedSw = Stopwatch.StartNew();
        var (primaryTenantId, primarySlug, primaryAdminToken) = await SeedAsync(plan);
        seedSw.Stop();
        output.WriteLine($"[{tier}] seed took {seedSw.Elapsed.TotalSeconds:0.0}s");

        var adminClient = Factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", primaryAdminToken);

        var storefront = Factory.CreateClient();
        storefront.DefaultRequestHeaders.Host = $"{primarySlug}.shop.demetrioq.com";

        // ── Warm-up — compile query plans ─────────────────────────────────
        await adminClient.GetAsync("/api/v1/tenants/dashboard");
        await adminClient.GetAsync("/api/v1/orders?page=1&pageSize=20");
        await storefront.GetAsync("/api/v1/storefront/products?page=1&pageSize=20");

        // Deep pagination = last page. At 1M orders this is OFFSET 1_000_000 — the
        // worst-case for SQL Server OFFSET/FETCH and SQLite LIMIT/OFFSET alike.
        int deepPage = Math.Max(2, plan.PrimaryOrders / 20);

        // ── Measure ────────────────────────────────────────────────────────
        var dashboardStats    = await TimeAsync("dashboard",           () => adminClient.GetAsync("/api/v1/tenants/dashboard"));
        var ordersStats       = await TimeAsync("orders",              () => adminClient.GetAsync("/api/v1/orders?page=1&pageSize=20"));
        var ordersDeepStats   = await TimeAsync("orders-deep",         () => adminClient.GetAsync($"/api/v1/orders?page={deepPage}&pageSize=20"));
        var storefrontStats   = await TimeAsync("storefront-products", () => storefront.GetAsync("/api/v1/storefront/products?page=1&pageSize=20"));

        // ── Report ─────────────────────────────────────────────────────────
        output.WriteLine("");
        output.WriteLine($"=== Perf benchmark [{tier}] ===");
        output.WriteLine($"Tenants={plan.TenantCount}  Primary: products={plan.PrimaryProducts:N0} customers={plan.PrimaryCustomers:N0} orders={plan.PrimaryOrders:N0}");
        output.WriteLine($"Other tenants each: products={plan.OtherProducts:N0} customers={plan.OtherCustomers:N0} orders={plan.OtherOrders:N0}");
        output.WriteLine($"Per-endpoint requests: {RequestsPerEndpoint}   deep-page: {deepPage:N0}");
        output.WriteLine("");
        output.WriteLine($"{"endpoint",-28} {"min(ms)",10} {"p50(ms)",10} {"p95(ms)",10} {"p99(ms)",10} {"max(ms)",10}");
        output.WriteLine(new string('-', 88));
        Print(dashboardStats);
        Print(ordersStats);
        Print(ordersDeepStats);
        Print(storefrontStats);
        output.WriteLine("");

        void Print(BenchmarkStats s) => output.WriteLine(
            $"{s.Name,-28} {s.MinMs,10:0.00} {s.P50Ms,10:0.00} {s.P95Ms,10:0.00} {s.P99Ms,10:0.00} {s.MaxMs,10:0.00}");

        dashboardStats.Failures.Should().Be(0);
        ordersStats.Failures.Should().Be(0);
        ordersDeepStats.Failures.Should().Be(0);
        storefrontStats.Failures.Should().Be(0);
    }

    // ── Seed ───────────────────────────────────────────────────────────────

    private async Task<(Guid PrimaryTenantId, string PrimarySlug, string PrimaryAdminToken)> SeedAsync(SeedPlan plan)
    {
        var runId = Guid.NewGuid().ToString("N")[..8];

        // Tenants via API — keeps onboarding/settings rows consistent.
        var tenants = new List<(Guid Id, string Slug)>(plan.TenantCount);
        for (int t = 0; t < plan.TenantCount; t++)
        {
            var slug = $"perf-{runId}-{t:D3}";
            var resp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = $"Perf {t}", slug });
            var created = await resp.Content.ReadFromJsonAsync<TenantResult>();
            tenants.Add((created!.TenantId, slug));
        }

        var (primaryId, primarySlug) = tenants[0];
        var adminToken = await CreateAdminAsync(primaryId, primarySlug, $"admin@{primarySlug}.test");

        // Products + customers via EF — sizes are modest (≤ ~15k each at high tier).
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            foreach (var (tenantId, slug) in tenants)
            {
                var isPrimary = tenantId == primaryId;
                var productCount  = isPrimary ? plan.PrimaryProducts  : plan.OtherProducts;
                var customerCount = isPrimary ? plan.PrimaryCustomers : plan.OtherCustomers;

                for (int p = 0; p < productCount; p++)
                {
                    var product = Product.Create(
                        tenantId,
                        name: $"Perf Product {p:D5}",
                        slug: $"perf-prod-{p:D5}",
                        description: "Bench item",
                        price: 10m + (p % 50),
                        stock: 10_000,
                        imageUrl: null,
                        sku: null);
                    db.Products.Add(product);
                }

                for (int c = 0; c < customerCount; c++)
                {
                    var email = $"cust{c:D5}.{slug}@perf.test";
                    var cust = Customer.Create(tenantId, email, "dummy-hash", $"Cust{c}", "Bench");
                    cust.VerifyEmail();
                    db.Customers.Add(cust);
                }
            }
            await db.SaveChangesAsync();
        }

        // Collect primary tenant's product + customer IDs for order seeding.
        // Other tenants' order seeding is also done here; we group by tenant.
        Dictionary<Guid, List<(Guid Id, decimal Price, string Name, string Slug, string? Sku)>> productsByTenant;
        Dictionary<Guid, List<Guid>> customersByTenant;
        var tenantIds = tenants.Select(t => t.Id).ToHashSet();
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            productsByTenant = db.Products.IgnoreQueryFilters()
                .Where(p => tenantIds.Contains(p.TenantId))
                .Select(p => new { p.TenantId, p.Id, p.Price, p.Name, p.Slug, p.Sku })
                .AsEnumerable()
                .GroupBy(x => x.TenantId)
                .ToDictionary(g => g.Key, g => g.Select(x => (x.Id, x.Price, x.Name, x.Slug, x.Sku)).ToList());

            customersByTenant = db.Customers.IgnoreQueryFilters()
                .Where(c => tenantIds.Contains(c.TenantId))
                .Select(c => new { c.TenantId, c.Id })
                .AsEnumerable()
                .GroupBy(x => x.TenantId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());
        }

        // Orders + items via raw SQL — millions of rows is too slow through EF.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = (SqliteConnection)db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            foreach (var (tenantId, _) in tenants)
            {
                var isPrimary = tenantId == primaryId;
                var orderCount = isPrimary ? plan.PrimaryOrders : plan.OtherOrders;
                if (orderCount == 0) continue;
                if (!productsByTenant.TryGetValue(tenantId, out var products) || products.Count == 0) continue;
                if (!customersByTenant.TryGetValue(tenantId, out var custs) || custs.Count == 0) continue;

                await BulkSeedOrdersAsync(connection, tenantId, custs, products, orderCount, seed: tenantId.GetHashCode());
            }
        }

        return (primaryId, primarySlug, adminToken);
    }

    /// <summary>
    /// Multi-row INSERT batched inside a single transaction. All seeded strings
    /// are controlled by the test harness (no user input), so literal interpolation
    /// is safe and ~5× faster than parameterized per-row inserts for this shape.
    /// </summary>
    private static async Task BulkSeedOrdersAsync(
        SqliteConnection conn,
        Guid tenantId,
        List<Guid> customerIds,
        List<(Guid Id, decimal Price, string Name, string Slug, string? Sku)> products,
        int orderCount,
        int seed)
    {
        var rnd = new Random(seed);
        const int batchSize = 500;
        var createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

        // Bulk load: disable FK checks while inserting. Data integrity is guaranteed
        // by construction (product/customer IDs were just materialized from the DB).
        // We re-enable after commit.
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = OFF;";
            await pragma.ExecuteNonQueryAsync();
        }

        using var tx = conn.BeginTransaction();

        var ordersSql = new StringBuilder(capacity: 256 * 1024);
        var itemsSql  = new StringBuilder(capacity: 256 * 1024);
        var lines = new (Guid Id, string Name, string Slug, string? Sku, decimal Unit, int Qty, decimal Line)[3];

        int ordersWritten = 0;
        while (ordersWritten < orderCount)
        {
            int thisBatch = Math.Min(batchSize, orderCount - ordersWritten);

            ordersSql.Clear();
            itemsSql.Clear();

            ordersSql.Append("INSERT INTO Orders (Id, TenantId, CustomerId, Number, Status, Subtotal, Total, ");
            ordersSql.Append("ShippingLine1, ShippingLine2, ShippingCity, ShippingRegion, ShippingPostalCode, ShippingCountry, ");
            ordersSql.Append("BillingLine1, BillingLine2, BillingCity, BillingRegion, BillingPostalCode, BillingCountry, ");
            ordersSql.Append("PaidAt, FulfilledAt, CanceledAt, PaymentProvider, PaymentSessionId, CreatedAt, UpdatedAt) VALUES ");

            itemsSql.Append("INSERT INTO OrderItems (Id, TenantId, OrderId, ProductId, ProductName, ProductSlug, ProductSku, UnitPrice, Quantity, LineTotal, CreatedAt, UpdatedAt) VALUES ");

            bool firstOrder = true;
            bool firstItem = true;

            for (int o = 0; o < thisBatch; o++)
            {
                var orderNum = ordersWritten + o;
                var orderId = Guid.NewGuid();
                var cust = customerIds[rnd.Next(customerIds.Count)];
                var number = $"PERF-{orderNum:D7}";

                var lineCount = rnd.Next(1, 4);
                decimal subtotal = 0m;
                for (int l = 0; l < lineCount; l++)
                {
                    var p = products[rnd.Next(products.Count)];
                    var qty = rnd.Next(1, 4);
                    var lineTotal = p.Price * qty;
                    subtotal += lineTotal;
                    lines[l] = (p.Id, p.Name, p.Slug, p.Sku, p.Price, qty, lineTotal);
                }

                // Status distribution (matches original): 70% paid, 10% fulfilled, rest pending.
                var roll = rnd.Next(1, 11);
                OrderStatus status = OrderStatus.Pending;
                string paidAtStr = "NULL";
                string fulfilledAtStr = "NULL";
                if (roll <= 7) { status = OrderStatus.Paid; paidAtStr = $"'{createdAt}'"; }
                if (roll == 10) { status = OrderStatus.Fulfilled; paidAtStr = $"'{createdAt}'"; fulfilledAtStr = $"'{createdAt}'"; }

                if (!firstOrder) ordersSql.Append(','); firstOrder = false;
                ordersSql.Append(CultureInfo.InvariantCulture, $"('{orderId}','{tenantId}','{cust}','{number}',{(int)status},{subtotal},{subtotal},");
                ordersSql.Append("'1 Way',NULL,'X','CA','00000','US',");
                ordersSql.Append("'1 Way',NULL,'X','CA','00000','US',");
                ordersSql.Append(CultureInfo.InvariantCulture, $"{paidAtStr},{fulfilledAtStr},NULL,NULL,NULL,'{createdAt}',NULL)");

                for (int l = 0; l < lineCount; l++)
                {
                    var ln = lines[l];
                    if (!firstItem) itemsSql.Append(','); firstItem = false;
                    var itemId = Guid.NewGuid();
                    // Seeded names/slugs contain no quotes, but be defensive.
                    var nameEsc = ln.Name.Replace("'", "''");
                    var slugEsc = ln.Slug.Replace("'", "''");
                    var skuPart = ln.Sku is null ? "NULL" : $"'{ln.Sku.Replace("'", "''")}'";
                    itemsSql.Append(CultureInfo.InvariantCulture,
                        $"('{itemId}','{tenantId}','{orderId}','{ln.Id}','{nameEsc}','{slugEsc}',{skuPart},{ln.Unit},{ln.Qty},{ln.Line},'{createdAt}',NULL)");
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = ordersSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = itemsSql.ToString();
                await cmd.ExecuteNonQueryAsync();
            }

            ordersWritten += thisBatch;
        }

        tx.Commit();

        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync();
        }
    }

    // ── Measurement ────────────────────────────────────────────────────────

    private static async Task<BenchmarkStats> TimeAsync(string name, Func<Task<HttpResponseMessage>> call)
    {
        var samples = new double[RequestsPerEndpoint];
        int failures = 0;
        var sw = new Stopwatch();
        for (int i = 0; i < RequestsPerEndpoint; i++)
        {
            sw.Restart();
            var resp = await call();
            sw.Stop();
            samples[i] = sw.Elapsed.TotalMilliseconds;
            if (!resp.IsSuccessStatusCode) failures++;
        }
        Array.Sort(samples);
        return new BenchmarkStats(
            Name: name,
            MinMs: samples[0],
            P50Ms: samples[RequestsPerEndpoint / 2],
            P95Ms: samples[(int)(RequestsPerEndpoint * 0.95)],
            P99Ms: samples[(int)(RequestsPerEndpoint * 0.99)],
            MaxMs: samples[^1],
            Failures: failures);
    }

    private record BenchmarkStats(string Name, double MinMs, double P50Ms, double P95Ms, double P99Ms, double MaxMs, int Failures);
}
