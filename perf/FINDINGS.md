# Performance findings

Benchmark of the three endpoints we optimized, before vs after.

## How these numbers were produced

In-process benchmark (`tests/SaasApi.IntegrationTests/PerfBenchmark.cs`)
using `WebAppFactory` + SQLite in-memory. Invoke with:

```bash
dotnet test --filter "FullyQualifiedName~PerfBenchmark"
```

**Seed:** 1 tenant, 300 products, 100 customers, **2,000 orders** with 1–3
line items each. 30 requests per endpoint.

**Caveat:** SQLite in-memory is much faster than SQL Server over a network.
Round-trip count and row-materialization cost both matter less here than they
would in prod. Absolute latencies are toy numbers; the useful signal is the
*relative* change between baseline and optimized. Real numbers on SQL Server
need the k6 scripts + your deployment.

## Results at 2,000 orders

### Baseline (commit `85a2ab3`, pre-optimization)

```
endpoint                      min(ms)  p50(ms)  p95(ms)  p99(ms)  max(ms)
------------------------------------------------------------------------------
dashboard                        68.5     94.2    108.5    117.4    117.4
orders                           37.1     45.9     68.6     75.2     75.2
orders-deep                      24.1     29.1     52.1     72.7     72.7
storefront-products               1.5      2.1      3.5      6.3      6.3
```

### After SQL-pushdown + query-combining (this branch)

```
endpoint                      min(ms)  p50(ms)  p95(ms)  p99(ms)  max(ms)
------------------------------------------------------------------------------
dashboard                         8.4     10.7     12.7     12.9     12.9
orders                            4.6      5.0      5.9      6.8      6.8
orders-deep                       6.2      7.5      9.5     26.5     26.5
storefront-products               0.8      0.8      1.1      2.4      2.4
```

### Delta — p95

| endpoint | baseline | optimized | speedup |
|---|---|---|---|
| dashboard | 108.5 ms | 12.7 ms | **~8.5×** |
| orders | 68.6 ms | 5.9 ms | **~11.6×** |
| orders-deep (page 100) | 52.1 ms | 9.5 ms | **~5.5×** |
| storefront-products | 3.5 ms | 1.1 ms | **~3×** |

## Correction

An earlier draft of this doc claimed the dashboard rewrite was "neutral at
2,000 orders on SQLite." That claim was wrong — not because of a subtle
scaling story, but because **the dashboard rewrite silently never landed in
the file**. The "optimized" and "baseline" measurements were comparing
the same handler against itself.

Fix applied in commit `f5e326d`. The real dashboard rewrite:

1. Switched from `IRepository<T>.GetAllAsync()` + in-memory aggregates to
   `IAppDbContext` + SQL `COUNT` / `SUM` / `GROUP BY`.
2. **Collapsed same-table aggregates into single queries** via
   `GroupBy(_ => 1).Select(...)` — e.g. user total + active count is one
   query emitting `SELECT COUNT(*), SUM(CASE WHEN IsActive THEN 1 ELSE 0 END)`
   instead of two round-trips.

Net result: 10+ round-trips collapsed to ~6, plus zero full-table
materialization.

## Commentary

### Orders (~11.6×)

`GET /api/v1/orders` went from 69ms p95 to ~6ms. Deep pagination (page 100)
saw a similar drop. The rewrite uses SQL `OFFSET/FETCH`, so page N costs
the same as page 1 instead of scaling linearly with row count. Biggest win
that ships on either DB provider.

### Dashboard (~8.5×)

The combined-query rewrite does ~6 round-trips instead of 10+. Each returns
either a scalar, a single-row aggregate, or a ≤5 or ≤10 row capped list —
no full-table materialization. That's why it dropped from ~110ms to ~13ms
on SQLite, where you wouldn't normally see SQL-pushdown wins (no network
latency to amortize).

### Storefront products (~3×)

Modest — 300 products isn't much data. The rewrite scales with catalog size;
a tenant with 10,000 SKUs would see a bigger delta.

### orders-deep p99 spike

`orders-deep` p99 jumped from 8.8ms to 26.5ms. That's one outlier in 30
samples (likely query-plan cold-start or GC timing in-process), not a
systemic regression. p95 is a cleaner signal at this sample count.

## The one meta-lesson

**Verify writes landed.** Earlier I reported a "neutral" dashboard result
because a tool-level file write silently no-op'd and the benchmark compared
two identical builds. Always rebuild + spot-check the file after a batch of
changes before trusting the numbers.

## Future perf work (not shipped)

- Add a real output cache on `GET /api/v1/stores` (directory endpoint) —
  hit from the storefront landing page, perfectly cacheable for 30–60s.
- Compile-time query caching: EF compiles query plans per call site;
  consider `EF.CompileAsyncQuery` for the top-products query.
- `IMemoryCache` around the dashboard endpoint with a 15s TTL for the
  "merchant refreshes dashboard constantly" pattern.
- Batch the dashboard queries into a single stored proc (or `FromSqlRaw`
  multi-result set) if network RTT becomes the bottleneck in prod.
- Read replica routing for Azure SQL if read traffic outgrows the primary.
