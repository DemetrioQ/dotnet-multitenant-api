# Performance benchmarks

k6 scripts that hammer three endpoints we expect to be the first to cave
under load, plus a seed endpoint to create realistic data volumes.

## Prerequisites

- [k6](https://k6.io/docs/get-started/installation/) on your PATH
- API running locally (`dotnet run --project src/SaasApi.API`)
- Super-admin credentials (see `SuperAdmin:Email` / `SuperAdmin:Password` user-secrets)

## Workflow

### 1. Seed realistic data

`POST /api/v1/dev/seed` is gated to the Development environment and super-admin only.
Log in to the API once to grab a super-admin JWT, then:

```bash
curl -X POST http://localhost:5299/api/v1/dev/seed \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{"tenants":3,"productsPerTenant":1000,"customersPerTenant":500,"ordersPerCustomer":3}'
```

Seeds are deterministic (`Randomizer.Seed = 42`) so product/customer names
repeat across runs — wipe the DB between runs if that matters.

Response includes a `duration` field so you can also spot insert throughput
regressions: seeding ~4,500 orders + ~18,000 line items should take a few seconds
on a local SQL Server; noticeable slowdown is the canary.

### 2. Run one of the k6 scripts

Each script takes its target config via env vars. Example — hammering the
dashboard for 30s at 10 virtual users:

```bash
k6 run perf/dashboard.js \
  -e API_URL=http://localhost:5299 \
  -e JWT=$MERCHANT_JWT \
  -e VUS=10 \
  -e DURATION=30s
```

The script prints `http_req_duration` percentiles at the end. Focus on **p95**
and **p99** — the worst request of every 20/100 is what a user actually feels.

### 3. Read the output

A healthy dashboard at seed-scale (~4,500 orders) looks like:

```
http_req_duration...........: avg=45ms  p(95)=120ms  p(99)=260ms
```

If p95 is over ~500ms, the handler is loading everything into memory and
aggregating client-side. See `perf/FINDINGS.md` for known offenders and their
SQL-pushdown fixes.

## Scripts

- `dashboard.js` — merchant dashboard stats (`GET /api/v1/tenants/dashboard`).
  Heaviest handler: loads all users, products, customers, orders, and orders'
  line items to compute revenue, AOV, top products.
- `orders.js` — merchant order list (`GET /api/v1/orders`). Pagination is
  computed in-memory after loading every order.
- `storefront-products.js` — public storefront catalog
  (`GET /api/v1/storefront/products`). Same in-memory-paging pattern, hit via
  the host header (no JWT).

## Reading the measurement

After each run, save the output alongside what you changed. Good perf-review
habit: `perf/results/2026-04-19-dashboard-baseline.txt` →
`perf/results/2026-04-19-dashboard-sql-pushdown.txt`, then a short note in
`FINDINGS.md` explaining what the delta came from.
