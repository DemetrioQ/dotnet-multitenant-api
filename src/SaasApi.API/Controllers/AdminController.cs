using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Admin.Commands.SetTenantStatus;
using SaasApi.Application.Features.Admin.Queries.GetAdminTenants;
using SaasApi.Application.Features.Admin.Queries.GetAllAuditLogs;
using SaasApi.Application.Features.Admin.Queries.GetAllProducts;
using SaasApi.Application.Features.Admin.Queries.GetPlatformStats;
using SaasApi.Application.Features.Admin.Queries.GetSignupsSeries;
using SaasApi.Application.Features.Admin.Queries.GetTenantUsers;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = RoleNames.SuperAdmin)]
[Route("api/v{version:apiVersion}/[controller]")]
public class AdminController(IMediator mediator) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetPlatformStats(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPlatformStatsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("stats/signups")]
    public async Task<IActionResult> GetSignupsSeries(
        [FromQuery] string? period,
        [FromQuery] string? entity,
        CancellationToken ct = default)
    {
        // period: 7d / 30d / 90d. Anything else → 30d. Cap 365d defensively.
        var days = ParsePeriod(period);
        if (!Enum.TryParse<SignupEntity>(entity, ignoreCase: true, out var entityEnum))
            entityEnum = SignupEntity.Tenants;

        var result = await mediator.Send(new GetSignupsSeriesQuery(entityEnum, days), ct);
        return Ok(result);
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetAdminTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await mediator.Send(new GetAdminTenantsQuery(page, pageSize), ct);
        return Ok(result);
    }

    private static int ParsePeriod(string? period)
    {
        if (string.IsNullOrWhiteSpace(period)) return 30;
        var trimmed = period.Trim().TrimEnd('d', 'D');
        if (int.TryParse(trimmed, out var n) && n is >= 1 and <= 365) return n;
        return 30;
    }

    [HttpGet("tenants/{tenantId}/users")]
    public async Task<IActionResult> GetTenantUsers(
        [FromRoute] Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await mediator.Send(new GetTenantUsersQuery(tenantId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAllAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await mediator.Send(new GetAllAuditLogsQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetAllProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await mediator.Send(new GetAllProductsQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPut("tenants/{tenantId}/status")]
    public async Task<IActionResult> SetTenantStatus(
        [FromRoute] Guid tenantId,
        [FromBody] SetTenantStatusRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new SetTenantStatusCommand(tenantId, request.IsActive), ct);
        return NoContent();
    }
}

public class SetTenantStatusRequest
{
    public bool IsActive { get; set; }
}
