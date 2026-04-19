using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaasApi.Domain.Entities;
using SaasApi.Infrastructure.Persistence;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace SaasApi.IntegrationTests;

/// <summary>
/// Relative benchmark of the three endpoints we optimized. Tagged with
/// [Trait("Category", "Benchmark")] so it's skipped by the default test run and
/// only picked up when explicitly filtered in.
///
/// SQLite in-memory is much faster than SQL Server + real network, so the
/// absolute latencies are toy numbers. The useful signal is the *shape* —
/// before-vs-after p95, and how p95 scales with data volume.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Benchmark")]
public class PerfBenchmark(WebAppFactory factory, ITestOutputHelper output) : IntegrationTestBase(factory)
{
    private const int OrdersToSeed = 2000;         // line items per order = 1..3
    private const int CustomersToSeed = 100;
    private const int ProductsToSeed = 300;
    private const int RequestsPerEndpoint = 30;

    private record TenantResult(Guid TenantId);
    private record ProductResult(Guid ProductId);
    private record LoginResult(string JwtToken, DateTime ExpiresAt);

    [Fact]
    public async Task Measure_Dashboard_Orders_Storefront()
    {
        // ── Setup ──────────────────────────────────────────────────────────
        var slug = "perf-" + Guid.NewGuid().ToString("N")[..8];
        var tenantResp = await Client.PostAsJsonAsync("/api/v1/tenants", new { name = "Perf Co", slug });
        var tenant = await tenantResp.Content.ReadFromJsonAsync<TenantResult>();
        var adminToken = await CreateAdminAsync(tenant!.TenantId, slug, $"admin@{slug}.test");

        var adminClient = Factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var productIds = new List<Guid>(ProductsToSeed);
        for (int i = 0; i < ProductsToSeed; i++)
        {
            var resp = await adminClient.PostAsJsonAsync("/api/v1/products", new
            {
                name = $"Perf Product {i:D4}",
                description = "Bench item",
                price = 10m + (i % 50),
                stock = 10_000
            });
            var pr = await resp.Content.ReadFromJsonAsync<ProductResult>();
            productIds.Add(pr!.ProductId);
        }

        // Seed customers + orders directly via DbContext to keep setup fast.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rnd = new Random(42);

            var customers = new List<Customer>(CustomersToSeed);
            for (int c = 0; c < CustomersToSeed; c++)
            {
                var email = $"cust{c:D4}.{slug}@perf.test";
                var cust = Customer.Create(tenant.TenantId, email, "dummy-hash", $"Cust{c}", "Bench");
                cust.VerifyEmail();
                customers.Add(cust);
                db.Customers.Add(cust);
            }
            await db.SaveChangesAsync();

            var products = db.Products.IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id)).ToList();
            var orders = new List<Order>(OrdersToSeed);
            var orderItems = new List<OrderItem>();
            for (int o = 0; o < OrdersToSeed; o++)
            {
                var cust = customers[rnd.Next(customers.Count)];
                var addr = new Address("1 Way", null, "X", "CA", "00000", "US");
                var number = $"PERF-{o:D6}";
                var order = Order.Create(tenant.TenantId, cust.Id, number, 0m, addr, addr);

                var lines = rnd.Next(1, 4);
                for (int l = 0; l < lines; l++)
                {
                    var p = products[rnd.Next(products.Count)];
                    var qty = rnd.Next(1, 4);
                    orderItems.Add(OrderItem.Create(tenant.TenantId, order.Id, p, qty));
                }

                var roll = rnd.Next(1, 11);
                if (roll <= 7) order.MarkPaid();
                if (roll == 10) order.MarkFulfilled();

                orders.Add(order);
            }
            db.Orders.AddRange(orders);
            db.OrderItems.AddRange(orderItems);
            await db.SaveChangesAsync();
        }

        // ── Measure ────────────────────────────────────────────────────────
        // Warm up so EF compiles the query plans we care about.
        await adminClient.GetAsync("/api/v1/tenants/dashboard");
        await adminClient.GetAsync("/api/v1/orders?page=1&pageSize=20");

        var storefront = Factory.CreateClient();
        storefront.DefaultRequestHeaders.Host = $"{slug}.shop.demetrioq.com";
        await storefront.GetAsync("/api/v1/storefront/products?page=1&pageSize=20");

        var dashboardStats = await TimeAsync("dashboard", () => adminClient.GetAsync("/api/v1/tenants/dashboard"));
        var ordersStats = await TimeAsync("orders", () => adminClient.GetAsync("/api/v1/orders?page=1&pageSize=20"));
        var ordersDeepStats = await TimeAsync("orders-deep", () => adminClient.GetAsync($"/api/v1/orders?page={OrdersToSeed / 20}&pageSize=20"));
        var storefrontStats = await TimeAsync("storefront-products", () => storefront.GetAsync("/api/v1/storefront/products?page=1&pageSize=20"));

        // ── Report ─────────────────────────────────────────────────────────
        output.WriteLine("");
        output.WriteLine("=== Perf benchmark results ===");
        output.WriteLine($"Seeded: tenants=1 products={ProductsToSeed} customers={CustomersToSeed} orders={OrdersToSeed} per-endpoint requests={RequestsPerEndpoint}");
        output.WriteLine("");
        output.WriteLine($"{"endpoint",-28} {"min(ms)",8} {"p50(ms)",8} {"p95(ms)",8} {"p99(ms)",8} {"max(ms)",8}");
        output.WriteLine(new string('-', 78));
        Print(dashboardStats);
        Print(ordersStats);
        Print(ordersDeepStats);
        Print(storefrontStats);
        output.WriteLine("");

        void Print(BenchmarkStats s) => output.WriteLine(
            $"{s.Name,-28} {s.MinMs,8:0.0} {s.P50Ms,8:0.0} {s.P95Ms,8:0.0} {s.P99Ms,8:0.0} {s.MaxMs,8:0.0}");

        // Sanity: endpoints actually succeeded.
        dashboardStats.Failures.Should().Be(0);
        ordersStats.Failures.Should().Be(0);
        ordersDeepStats.Failures.Should().Be(0);
        storefrontStats.Failures.Should().Be(0);
    }

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
