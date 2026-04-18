using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Storefront.Cart.Commands.AddCartItem;
using SaasApi.Application.Features.Storefront.Cart.Commands.ClearCart;
using SaasApi.Application.Features.Storefront.Cart.Commands.RemoveCartItem;
using SaasApi.Application.Features.Storefront.Cart.Commands.UpdateCartItemQuantity;
using SaasApi.Application.Features.Storefront.Cart.Queries.GetCart;

namespace SaasApi.API.Controllers;

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "CustomerOnly")]
[Route("api/v{version:apiVersion}/storefront/cart")]
public class CartController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCartQuery(), ct);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateItem(
        [FromRoute] Guid productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateCartItemQuantityCommand(productId, request.Quantity), ct);
        return Ok(result);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid productId, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveCartItemCommand(productId), ct);
        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await mediator.Send(new ClearCartCommand(), ct);
        return NoContent();
    }
}
