using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.AuditLog.Queries.GetAuditLog;

namespace SaasApi.API.Controllers;

[ApiController]
[Authorize(Roles = "admin,super-admin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuditController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLog([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await mediator.Send(new GetAuditLogQuery(page, pageSize), ct);
        return Ok(result);
    }
}
