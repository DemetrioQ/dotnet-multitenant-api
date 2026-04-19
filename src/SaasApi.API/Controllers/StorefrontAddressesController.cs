using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Storefront.Addresses.Commands.CreateAddress;
using SaasApi.Application.Features.Storefront.Addresses.Commands.DeleteAddress;
using SaasApi.Application.Features.Storefront.Addresses.Commands.UpdateAddress;
using SaasApi.Application.Features.Storefront.Addresses.Queries.GetMyAddresses;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v{version:apiVersion}/storefront/addresses")]
public class StorefrontAddressesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyAddresses(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyAddressesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetMyAddresses), new { }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAddressRequest request, CancellationToken ct)
    {
        var cmd = new UpdateAddressCommand(id, request.Label, request.Address, request.IsDefaultShipping, request.IsDefaultBilling);
        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteAddressCommand(id), ct);
        return NoContent();
    }
}

public class UpdateAddressRequest
{
    public string? Label { get; set; }
    public SaasApi.Application.Features.Storefront.Addresses.AddressInput Address { get; set; } = default!;
    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }
}
