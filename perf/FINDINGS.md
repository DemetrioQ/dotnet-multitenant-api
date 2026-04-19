# Performance findings

Notes on the three hot paths we rewrote after seeding realistic data, the
anti-pattern each one was using, and what the SQL-pushed rewrite looks like.

## Shared anti-pattern: "load all → filter/paginate in memory"

Before the rewrites, all three handlers called something like
`IRepository<T>.FindAsync(predicate)` — which in the existing repository does
`DbSet<T>.Where(predicate).ToListAsync()` with no pagination or projection.
Then the handler applied `.OrderBy().Skip().Take()` or ran LINQ aggregates
client-side.

At 10 rows this is fine. At 10,000 it loads every matching row over the wire
(plus every FK-referenced row for aggregates) and allocates big object graphs
— latency scales linearly with table size, not page size.

## 1. `GET /api/v1/tenants/dashboard`

Before: six `GetAllAsync` / `FindAsync` calls (users, products, customers,
orders, paid order items, audit) each loading the full tenant-scoped set.
With 4,500 orders + 18,000 order items per tenant, a single dashboard request
pulled ~30k rows across half a dozen queries just to return ~20 numbers.

After: one `COUNT(*)` per stat card (pushed to SQL — zero rows returned),
one `GROUP BY 1 → SUM(total)` for revenue/AOV, one `GROUP BY productId ORDER
BY SUM(lineTotal) DESC TAKE 5` for the top-products widget, one `TAKE 10`
for recent activity.

Expected shape of the win: ~10× at 1,000 orders, ~50× at 10,000+.

## 2. `GET /api/v1/orders?page=N`

Before: `orderRepo.FindAsync(o => o.Status == status)` (or `GetAllAsync`),
then `.OrderByDescending.Skip.Take` in memory. For page 1 of 20 this
loaded every order in the tenant. A second query loaded every matching
`OrderItem` for the full set to compute `ItemCount` per row.

After: SQL `ORDER BY CreatedAt DESC OFFSET … FETCH NEXT pageSize ROWS`,
inner join on Customer, correlated subquery for the item sum. One query
returns a perfectly-sized page; total count is a separate `COUNT(*)`.

Expected shape of the win: the "deep pagination" scenario is the worst case
before (page 50 still loads the full set), and the rewrite cost is flat
regardless of page.

## 3. `GET /api/v1/storefront/products?page=N`

Same pattern as merchant orders: before, we loaded every active product
into memory and paginated. After, the `Where/OrderBy/Skip/Take/Select` chain
runs as a single SQL query returning only the page.

Low-impact at small scale (product catalogs are smaller than order tables),
but the fix is trivial and it removes another "time bomb" that would have
bit a tenant with 5,000 SKUs.

## How to measure before vs after on your own hardware

We've tagged the rewrite as `perf-pushdown` in git. To see the delta:

```bash
# baseline (before the rewrite)
git checkout <last-commit-before-perf>
dotnet run --project src/SaasApi.API &
# (seed data; run k6 script; save results)

git checkout main
dotnet run --project src/SaasApi.API &
# (re-run the same k6 script; save results)
```

Compare `http_req_duration` p95 / p99 between the two runs.

## Why `IAppDbContext` showed up

The rewrites needed real `IQueryable<T>` access so EF could translate the
LINQ into SQL. The existing `IRepository<T>.FindAsync(predicate)` materializes
results before you can chain further operators, so it can't do SQL-side
pagination or aggregation.

We added a minimal `IAppDbContext` interface in `Application/Common/Interfaces`
exposing only the `DbSet<T>`s the optimized handlers need. Infrastructure's
`AppDbContext` implements it. Pragmatic tradeoff: Application now has a
`Microsoft.EntityFrameworkCore` package reference. We kept `IRepository<T>`
as the default for simpler write-side flows.

## Future perf work (not shipped)

- Add a real output cache on `GET /api/v1/stores` (directory endpoint) — hit
  from the storefront landing page, perfectly cacheable for 30–60s.
- Compile-time query caching: EF compiles query plans per call site; for
  the top-products query, consider `EF.CompileAsyncQuery`.
- Read replica routing if Azure SQL is the bottleneck (Premium-tier feature).
- `IMemoryCache` around the dashboard endpoint with a short TTL (~15s) for
  the unavoidable "merchant refreshes dashboard constantly" pattern.
