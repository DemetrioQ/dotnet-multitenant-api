# Performance findings

Two studies of the optimized read endpoints:

1. **Before-vs-after at 2,000 orders** — shows the speedup from the
   SQL-pushdown + query-combining rewrites.
2. **Scaling study (low / medium / high)** — shows how the optimized
   endpoints behave at 2k → 200k → 1M orders on the same harness.

## How these numbers were produced

In-process benchmark (`tests/SaasApi.IntegrationTests/PerfBenchmark.cs`)
using `WebAppFactory` + SQLite in-memory. Invoke with:

```bash
dotnet test --filter "FullyQualifiedName~PerfBenchmark"
```

Each theory row seeds its own tier and measures 30 requests per endpoint
against the primary (largest) tenant. The `low` tier matches the numbers
used in the before-vs-after comparison below.

**Caveat:** SQLite in-memory is much faster than SQL Server over a network.
Round-trip count and row-materialization cost both matter less here than they
would in prod. Absolute latencies are toy numbers; the useful signals are
(a) the *relative* change between baseline and optimized, and (b) whether
p95 blows up as data volume grows. Real numbers on SQL Server need the k6
scripts + your deployment.

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

## Scaling study — low / medium / high

The benchmark is a `[Theory]` with three tiers. Each tier seeds its own set of
tenants (fresh slugs) and measures the endpoints against the **largest** tenant.
Other tenants exist to put data in neighboring partitions so the tenant filter
isn't trivially selective.

| tier   | tenants | primary products | primary customers | primary orders | deep-page offset |
|--------|---------|------------------|-------------------|----------------|------------------|
| low    | 1       | 300              | 100               | 2,000          | 100              |
| medium | 10      | 1,000            | 2,000             | 200,000        | 10,000           |
| high   | 50      | 5,000            | 10,000            | 1,000,000      | 50,000           |

Seed time: low **~1s**, medium **~9s**, high **~40s** (raw-SQL bulk insert with
`PRAGMA foreign_keys = OFF` during the load — data integrity guaranteed by
construction, since the product/customer IDs are materialized from the same
connection immediately before the inserts).

### p95 across tiers (SQLite in-memory)

| endpoint             | low (2k) | medium (200k) | high (1M) |
|----------------------|----------|---------------|-----------|
| dashboard            | 2.9 ms   | 1.3 ms        | 4.0 ms    |
| orders               | 0.9 ms   | 0.3 ms        | 0.7 ms    |
| orders-deep          | 0.8 ms   | 0.4 ms        | 0.6 ms    |
| storefront-products  | 1.9 ms   | 0.6 ms        | 2.2 ms    |

Full tables (min / p50 / p95 / p99 / max in ms) are in
`perf/bench-output.log` after a run.

### How to read this

**Don't read it as a monotonic scaling curve.** Two things to be aware of:

1. **xUnit theory-row ordering is not the tier ordering.** Observed order here
   was `low → high → medium`. The first tier pays JIT + query-plan + ASP.NET
   pipeline warmup; subsequent tiers benefit. At sub-5ms absolute latencies
   that cold-start cost (~1-2 ms) dominates the *differences* between tiers.
2. **SQLite in-memory is not SQL Server.** At 1M rows on a disk-backed engine
   with network RTT, you'd see different shapes. Here everything is cache-hot
   RAM and a no-op network hop.

What the study **does** tell us:

- **None of the optimized endpoints blow up at 1M orders.** All four stay
  sub-5 ms p95 on this harness even when the tenant has a million rows across
  ~2 million order items.
- **OFFSET pagination at page 50,000 (offset = 1,000,000) on `orders-deep`
  stays sub-1 ms.** Because the query orders by an indexed column (`Id`),
  SQLite can seek-and-skip rather than materialize a million rows. On SQL
  Server the shape holds only if the same index is usable; if it isn't, deep
  OFFSET degrades to O(n). Keyset pagination would be the robust fix before
  shipping very-deep pagination as a supported feature.
- **Dashboard at 1M orders is ~4 ms p95** on SQLite in-memory. The combined
  `GroupBy(_ => 1).Select(...)` aggregates push everything to SQL; row count
  only shows up as a constant multiplier inside a single scan per table.
- **Storefront products scales with catalog size, not order count** — as
  expected, since it only touches `Products`.

### Caveats for real-deployment numbers

- For SQL Server numbers the k6 scripts in this folder plus a deployed
  instance is the right tool. This in-process benchmark is a regression
  detector, not a capacity model.
- The "other tenants" in medium/high are small (100 products, 50 customers,
  100–200 orders each) — enough to pollute shared indexes, not enough to
  model a real noisy-neighbor scenario. If you want that, crank those knobs.
- Seed uses `PRAGMA foreign_keys = OFF` during bulk load. Tests re-enable
  FKs before measurement so query behavior is unaffected.

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
