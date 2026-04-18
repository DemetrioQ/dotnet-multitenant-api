using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Storefront.Orders.Commands.Checkout;
using SaasApi.Application.Features.Storefront.Orders.Commands.CreateCheckoutSession;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v{version:apiVersion}/storefront/checkout")]
public class CheckoutController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command, CancellationToken ct)
    {
        var order = await mediator.Send(command, ct);
        return CreatedAtAction("GetOrderById",
            controllerName: "StorefrontOrders",
            routeValues: new { id = order.Id, version = "1.0" },
            value: order);
    }

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] CreateCheckoutSessionCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}
