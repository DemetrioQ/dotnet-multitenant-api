using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Tenants.Commands.CreateTenant;
using SaasApi.Application.Features.Tenants.Commands.DeactivateTenant;
using SaasApi.Application.Features.Tenants.Commands.UpdateTenant;
using SaasApi.Application.Features.Tenants.Queries.GetTenantById;
using SaasApi.Application.Features.Tenants.Queries.GetOnboardingStatus;
using SaasApi.Application.Features.Tenants.Queries.GetTenantDashboard;
using SaasApi.Application.Features.Tenants.Queries.GetTenants;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TenantsController(IMediator mediator, ICurrentTenantService tenantService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(CreateTenant), new { tenantId = result.TenantId }, result);
        }

        [HttpGet("onboarding")]
        [Authorize]
        public async Task<IActionResult> GetOnboardingStatus(CancellationToken ct)
        {
            var result = await mediator.Send(new GetOnboardingStatusQuery(), ct);
            return Ok(result);
        }

        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantDashboardQuery(), ct);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyTenant(CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantByIdQuery(tenantService.TenantId), ct);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.SuperAdmin)]
        public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var result = await mediator.Send(new GetTenantsQuery(page, pageSize), ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = RoleNames.SuperAdmin)]
        public async Task<IActionResult> GetTenantById([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantByIdQuery(id), ct);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleNames.AdminAndAbove)]
        public async Task<IActionResult> UpdateTenant([FromRoute] Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
        {
            var command = new UpdateTenantCommand(id, request.Name, request.SupportEmail, request.WebsiteUrl);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleNames.AdminAndAbove)]
        public async Task<IActionResult> DeactivateTenant([FromRoute] Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeactivateTenantCommand(id), ct);
            return NoContent();
        }
    }
}
