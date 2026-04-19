# SaaS API

A multi-tenant SaaS e-commerce backend — "Shopify-for-X." One API hosts many
merchant tenants, each served on its own subdomain (`{slug}.shop.example.com`),
with a merchant admin and a customer-facing storefront sharing a single
ASP.NET Core application.

## What it does

- **Merchant side:** tenant signup, product catalog, order fulfillment,
  customer list, onboarding status, dashboard analytics.
- **Storefront side:** customer signup/login, product browsing, cart,
  checkout via Stripe (or a simulation provider), order history.
- **Platform:** JWT auth with refresh-token rotation and family tracking,
  email verification, password reset, rate limiting on auth endpoints,
  role-based authorization, global exception mapping to ProblemDetails,
  low-stock alerts, structured logging enriched with tenant/user context.

## Stack

- **Runtime:** .NET 9, ASP.NET Core
- **Data:** EF Core 9 + SQL Server (SQLite in-memory for tests)
- **CQRS:** MediatR + FluentValidation
- **Auth:** JWT + refresh tokens (BCrypt password hashing)
- **Payments:** Stripe (pluggable `IPaymentService`, simulation provider for dev)
- **Docs:** OpenAPI + Scalar (`/scalar/v1`, and `/` redirects there)
- **Logging:** Serilog (console + request logging with tenant/user enrichment)
- **Tests:** xUnit + FluentAssertions + WebApplicationFactory

## Architecture

Clean Architecture, four projects:

```
src/
  SaasApi.Domain/         entities, domain interfaces (no dependencies)
  SaasApi.Application/    MediatR commands/queries, validators, DTOs
  SaasApi.Infrastructure/ EF Core, repositories, JWT, BCrypt, payment services
  SaasApi.API/            controllers, middleware, DI composition root
tests/
  SaasApi.IntegrationTests/   WebApplicationFactory + SQLite
```

Every feature lives under `Features/{Domain}/Commands|Queries/{FeatureName}/`
and contains a Command/Query, Handler, and Validator where applicable.
DTOs use a `static FromEntity(T)` factory.

## Multi-tenancy

Tenant is resolved per request, in order:

1. `tenant_id` JWT claim (merchant and customer tokens carry this)
2. Host-based: leftmost label of a host matching `Storefront:HostSuffix`
   (e.g. `acme.shop.example.com` → slug `acme`)
3. Dev-only `?storeSlug=acme` fallback when host is `localhost`

Lookups are cached in `IMemoryCache` (10 min hits / 30 s misses). EF Core
global query filters scope every tenant entity automatically, so handler
code never writes a `TenantId ==` clause.

## Running locally

```bash
docker compose up --build
```

Brings up SQL Server and the API on `http://localhost:5000`. Navigate to
`/` to land on the API reference (Scalar).

For a non-Docker run:

```bash
dotnet run --project src/SaasApi.API
```

Requires a SQL Server instance reachable via
`ConnectionStrings:DefaultConnection` in `appsettings.Development.json` or
environment variable.

## Tests

```bash
dotnet test
```

Integration tests swap SQL Server for a persistent in-memory SQLite
connection. All test classes share a single `WebApplicationFactory` via
`[Collection("Integration")]`.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Secret` / `Jwt:Issuer` / `Jwt:Audience` | JWT signing and validation |
| `Storefront:HostSuffix` | Subdomain suffix for tenant resolution (e.g. `.shop.example.com`) |
| `App:FrontendUrl` | Allowed CORS origin for the admin dashboard |
| `Payments:Provider` | `Stripe` or `Simulation` |
| `Payments:Stripe:*` | Stripe secret key, webhook secret, success/cancel URLs |

## Key endpoints

- `POST /api/v1/auth/register`, `login`, `refresh`, `logout`
- `POST /api/v1/auth/forgot-password`, `reset-password`
- `GET  /api/v1/auth/verify-email`, `POST /resend-verification`
- `GET/POST/PUT/DELETE /api/v1/products`
- `GET /api/v1/tenants/dashboard`, `GET /api/v1/tenants/onboarding`
- `GET /api/v1/orders`, `POST /{id}/fulfill`, `POST /{id}/cancel`
- `GET /api/v1/customers`
- `GET /api/v1/storefront/store`, `GET /products`, `GET /products/{slug}`
- `POST /api/v1/cart/items`, `POST /api/v1/checkout`

Full schema is available at `/scalar/v1`.

## CI/CD

GitHub Actions: restore → build → integration tests → Docker image build
and push on `main` → deploy to Oracle VM.
