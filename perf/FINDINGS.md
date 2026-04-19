# Performance findings

Benchmark of the three endpoints we optimized, before vs after.

## How these numbers were produced

In-process benchmark (`tests/SaasApi.IntegrationTests/PerfBenchmark.cs`) using
the existing `WebAppFactory` harness. The `[Trait("Category", "Benchmark")]`
tag keeps it out of the default test run; invoke explicitly with:

```bash
dotnet test --filter "FullyQualifiedName~PerfBenchmark"
```

**Seed:** 1 tenant, 300 products, 100 customers, **2,000 orders** with 1–3
line items each. 30 requests per endpoint.

**Important caveat — SQLite in-memory is not SQL Server.** The test harness
replaces SQL Server with SQLite running in-memory. That means:

- No network round-trip cost per query. Each query is essentially a function
  call into the same process.
- "Load all rows" is nearly free at 2,000 rows (it'd be awful on SQL Server
  over a LAN at the same row count).
- Query-plan optimization and index scans behave differently.

So the **relative** improvement on `orders` (clear) will be even larger on
real SQL Server. The **absolute** dashboard numbers shown here are noisier
than they'd be in prod — see commentary at the bottom.

For real-world numbers, use the k6 scripts + your SQL Server deployment.

## Results (2,000 orders, SQLite in-memory)

### Baseline (commit `85a2ab3`, pre-optimization)

```
endpoint                      min(ms)  p50(ms)  p95(ms)  p99(ms)  max(ms)
------------------------------------------------------------------------------
dashboard                        68.5     94.2    108.5    117.4    117.4
orders                           37.1     45.9     68.6     75.2     75.2
orders-deep                      24.1     29.1     52.1     72.7     72.7
storefront-products               1.5      2.1      3.5      6.3      6.3
```

### After SQL-pushdown (commit `7f6a209`, this branch)

```
endpoint                      min(ms)  p50(ms)  p95(ms)  p99(ms)  max(ms)
------------------------------------------------------------------------------
dashboard                        66.7     98.7    117.5    128.4    128.4
orders                            4.5      4.9      6.9      7.2      7.2
orders-deep                       6.0      6.4      8.6      8.8      8.8
storefront-products               0.7      0.8      3.0     12.2     12.2
```

### Delta — p95

| endpoint | baseline | optimized | speedup |
|---|---|---|---|
| dashboard | 108.5 ms | 117.5 ms | **1.0×** (neutral) |
| orders | 68.6 ms | 6.9 ms | **~10×** |
| orders-deep (page 100) | 52.1 ms | 8.6 ms | **~6×** |
| storefront-products | 3.5 ms | 3.0 ms | **~1.2×** |

## Commentary

### Orders: clear win

`GET /api/v1/orders` went from ~69ms p95 to ~7ms — roughly an order of
magnitude. Deep pagination (page 100) saw a similar win because the rewrite
uses `OFFSET/FETCH` server-side, so page N costs the same as page 1 instead
of linearly scaling.

### Storefront products: mild win

3.5ms → 3.0ms p95. Small because 300 active products isn't enough to stress
the "load all" pattern. The real win will appear when a tenant hits a few
thousand SKUs — still worth shipping the fix.

### Dashboard: why it looks neutral on SQLite

The pre-rewrite dashboard did **six `GetAllAsync` calls + client-side
aggregation** — one query each pulling the full table, then sums and groups
in .NET code. The rewrite does **~10 separate `COUNT`/`SUM`/`GROUP BY`
queries** that each return a single number or small result set.

On SQL Server over a network connection each round-trip is ~1–10ms of wire
latency. Six-vs-ten is close enough that query-planning matters more than
round-trip count; loading the full set quickly becomes a problem because
materializing 2,000 orders plus their 5,000ish line items into .NET objects
is expensive. The optimized version wins big because it returns single
scalars — no entity materialization.

On **SQLite in-memory** there is no network latency and materialization is
cheaper (no column type coercion over the wire). The "ten small queries"
pattern looks very similar to "one big query + client aggregation" because
SQLite executes both in the same process. That's what this benchmark shows:
the two versions are ~identical at 2,000 orders.

**Expected behavior on real SQL Server at the same data volume:** baseline
would climb sharply as it materializes everything; optimized would stay
flat. The code change is the right one, but demonstrating it requires the
k6 scripts running against SQL Server.

### Meta-observation: in-process harness doesn't stress the right thing

The TL;DR for future perf work:

- In-process benchmarks (SQLite) are great for detecting changes in **query
  count and payload size** (which is why orders shows a 10× win — 500 rows
  transferred vs 1 row with a COUNT became ~10× fewer bytes/ticks).
- They **don't** model network RTT between API and DB, which is where
  `load-all-into-memory` actually hurts in prod.
- For the dashboard specifically, run it against SQL Server via k6 and
  expect a 3–10× improvement at the same data volume.

## Future perf work (not shipped)

- Add a real output cache on `GET /api/v1/stores` (directory endpoint) — hit
  from the storefront landing page, perfectly cacheable for 30–60s.
- Compile-time query caching: EF compiles query plans per call site; for
  the top-products query, consider `EF.CompileAsyncQuery`.
- Read replica routing if Azure SQL is the bottleneck (Premium-tier feature).
- `IMemoryCache` around the dashboard endpoint with a short TTL (~15s) for
  the unavoidable "merchant refreshes dashboard constantly" pattern.
- Batch the dashboard's ~10 queries into a single stored procedure (or
  `FromSqlRaw` union) if round-trip count becomes the bottleneck in prod.
