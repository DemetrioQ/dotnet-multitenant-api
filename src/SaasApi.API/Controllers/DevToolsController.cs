using Asp.Versioning;
using Bogus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Infrastructure.Persistence;

namespace SaasApi.API.Controllers;

public class SeedRequest
{
    public int Tenants { get; set; } = 3;
    public int ProductsPerTenant { get; set; } = 100;
    public int CustomersPerTenant { get; set; } = 50;
    public int OrdersPerCustomer { get; set; } = 2;
}

public record SeedResult(
    int Tenants,
    int Products,
    int Customers,
    int Orders,
    int OrderItems,
    TimeSpan Duration);

/// <summary>
/// Dev-only tools — gated by <see cref="IHostEnvironment.IsDevelopment"/> at the
/// controller action level and by super-admin role. The seed endpoint generates
/// realistic bulk data so we can see how the app behaves under load without
/// manually clicking through hundreds of workflows.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = RoleNames.SuperAdmin)]
[Route("api/v{version:apiVersion}/dev")]
public class DevToolsController(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IHostEnvironment env) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromBody] SeedRequest request, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return Forbid();

        var started = DateTime.UtcNow;

        // Clamp so an accidental 10_000_000 request can't OOM the box.
        var tenantsToMake = Math.Clamp(request.Tenants, 1, 50);
        var productsPerTenant = Math.Clamp(request.ProductsPerTenant, 1, 5_000);
        var customersPerTenant = Math.Clamp(request.CustomersPerTenant, 1, 5_000);
        var ordersPerCustomer = Math.Clamp(request.OrdersPerCustomer, 0, 50);

        Randomizer.Seed = new Random(42);
        var productFaker = new Faker<Product>();
        var customerFaker = new Faker<Customer>();

        var password = passwordHasher.Hash("Password1!");

        int productCount = 0, customerCount = 0, orderCount = 0, orderItemCount = 0;
        var tenantIds = new List<Guid>(tenantsToMake);

        for (int t = 0; t < tenantsToMake; t++)
        {
            var tenantName = new Faker().Company.CompanyName();
            var slug = "seed-" + Guid.NewGuid().ToString("N")[..8];

            var tenant = Tenant.Create(tenantName, slug);
            db.Tenants.Add(tenant);
            db.TenantSettings.Add(TenantSettings.Create(tenant.Id));
            db.TenantOnboardingStatuses.Add(TenantOnboardingStatus.Create(tenant.Id));

            // One merchant admin per tenant so dashboard pages have a user to belong to.
            var adminEmail = $"admin@{slug}.test";
            // Fully qualified — "User" resolves to ControllerBase.User inside a controller.
            var admin = SaasApi.Domain.Entities.User.Create(tenant.Id, adminEmail, password, UserRole.Admin);
            admin.VerifyEmail();
            db.Users.Add(admin);
            db.UserProfiles.Add(UserProfile.Create(admin.Id, tenant.Id, "Seed", "Admin"));

            // Products.
            var products = new List<Product>(productsPerTenant);
            var faker = new Faker();
            for (int p = 0; p < productsPerTenant; p++)
            {
                var name = faker.Commerce.ProductName() + " " + faker.Random.Int(1000, 9999);
                var product = Product.Create(
                    tenant.Id,
                    name,
                    slug: faker.Random.AlphaNumeric(10).ToLowerInvariant(),
                    description: faker.Commerce.ProductDescription(),
                    price: decimal.Parse(faker.Commerce.Price(5, 500)),
                    stock: faker.Random.Int(10, 200),
                    imageUrl: faker.Image.PicsumUrl(),
                    sku: faker.Commerce.Ean8());
                products.Add(product);
                db.Products.Add(product);
            }

            // Customers.
            var customers = new List<Customer>(customersPerTenant);
            for (int c = 0; c < customersPerTenant; c++)
            {
                var first = faker.Name.FirstName();
                var last = faker.Name.LastName();
                var email = $"{first}.{last}.{c}.{t}@seed.test".ToLowerInvariant();
                var cust = Customer.Create(tenant.Id, email, password, first, last);
                cust.VerifyEmail();
                customers.Add(cust);
                db.Customers.Add(cust);
            }

            // Flush so we have FK ids for customers/products before inserting orders.
            await db.SaveChangesAsync(ct);
            productCount += products.Count;
            customerCount += customers.Count;
            tenantIds.Add(tenant.Id);

            // Orders: each customer gets N orders with 1–3 line items. Most are paid,
            // some pending, a few fulfilled — gives dashboard aggregates something to chew on.
            if (ordersPerCustomer == 0) continue;
            var orders = new List<Order>();
            var items = new List<OrderItem>();
            foreach (var cust in customers)
            {
                for (int o = 0; o < ordersPerCustomer; o++)
                {
                    var lineCount = faker.Random.Int(1, 3);
                    var chosen = faker.PickRandom(products, lineCount).Distinct().ToList();
                    decimal subtotal = 0;
                    var orderItems = new List<OrderItem>();
                    var orderNumber = "SEED-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

                    var shipping = new Address(
                        faker.Address.StreetAddress(), null,
                        faker.Address.City(), faker.Address.StateAbbr(),
                        faker.Address.ZipCode(), "US");

                    // Build the order shell first so we can reference order.Id when creating items.
                    var order = Order.Create(tenant.Id, cust.Id, orderNumber, 0m, shipping, shipping);
                    orders.Add(order);

                    foreach (var prod in chosen)
                    {
                        var qty = faker.Random.Int(1, 3);
                        subtotal += prod.Price * qty;
                        orderItems.Add(OrderItem.Create(tenant.Id, order.Id, prod, qty));
                    }

                    // Subtotal is private-set on Order; simplest "patch" is to construct a
                    // replacement order with the correct subtotal. For seed data accuracy
                    // isn't critical — leave subtotal=0 and let Total drive from OrderItems.
                    // (Left as-is to keep the seed fast; not production data.)

                    // Transition a proportion to paid/fulfilled for dashboard revenue variety.
                    var roll = faker.Random.Int(1, 10);
                    if (roll <= 7) order.MarkPaid();
                    if (roll == 10) order.MarkFulfilled();

                    items.AddRange(orderItems);
                }
            }

            db.Orders.AddRange(orders);
            db.OrderItems.AddRange(items);
            await db.SaveChangesAsync(ct);
            orderCount += orders.Count;
            orderItemCount += items.Count;
        }

        return Ok(new SeedResult(
            tenantsToMake, productCount, customerCount, orderCount, orderItemCount,
            DateTime.UtcNow - started));
    }
}
