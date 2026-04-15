using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Tenants.Commands.CreateTenant;
using SaasApi.Application.Features.Tenants.Commands.DeactivateTenant;
using SaasApi.Application.Features.Tenants.Commands.UpdateTenant;
using SaasApi.Application.Features.Tenants.Queries.GetTenantById;
using SaasApi.Application.Features.Tenants.Queries.GetTenants;

namespace SaasApi.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(CreateTenant), new { tenantId = result.TenantId }, result);
        }


        [HttpGet]
        public async Task<IActionResult> GetTenants(CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantsQuery(), ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTenantById([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetTenantByIdQuery(id), ct);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateTenant([FromRoute] Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
        {
            var command = new UpdateTenantCommand(id, request.Name);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeactivateTenant([FromRoute] Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeactivateTenantCommand(id), ct);
            return NoContent();
        }

    }
}
