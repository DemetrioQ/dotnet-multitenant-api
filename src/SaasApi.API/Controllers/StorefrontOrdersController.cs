using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Storefront.Orders.Queries.GetMyOrders;
using SaasApi.Application.Features.Storefront.Orders.Queries.GetOrderById;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v{version:apiVersion}/storefront/orders")]
public class StorefrontOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] GetMyOrdersQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetOrderById")]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id), ct);
        return Ok(result);
    }
}
