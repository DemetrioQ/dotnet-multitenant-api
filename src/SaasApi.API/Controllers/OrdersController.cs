using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.MerchantOrders.Commands.CancelOrder;
using SaasApi.Application.Features.MerchantOrders.Commands.FulfillOrder;
using SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrderById;
using SaasApi.Application.Features.MerchantOrders.Queries.GetMerchantOrders;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/orders")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] GetMerchantOrdersQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetMerchantOrderByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/fulfill")]
    public async Task<IActionResult> Fulfill([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new FulfillOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelOrderCommand(id), ct);
        return Ok(result);
    }
}
