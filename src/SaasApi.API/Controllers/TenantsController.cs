using MediatR;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Tenants.Commands.CreateTenant;

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

    }
}
