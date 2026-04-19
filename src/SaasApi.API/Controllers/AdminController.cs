using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Admin.Commands.SetTenantStatus;
using SaasApi.Application.Features.Admin.Queries.GetAllAuditLogs;
using SaasApi.Application.Features.Admin.Queries.GetAllProducts;
using SaasApi.Application.Features.Admin.Queries.GetPlatformStats;
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
