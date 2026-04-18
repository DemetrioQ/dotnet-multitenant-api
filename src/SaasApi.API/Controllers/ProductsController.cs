using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Products.Commands.CreateProduct;
using SaasApi.Application.Features.Products.Commands.DeleteProduct;
using SaasApi.Application.Features.Products.Commands.SetProductStatus;
using SaasApi.Application.Features.Products.Commands.UpdateProduct;
using SaasApi.Application.Features.Products.Queries.GetProductById;
using SaasApi.Application.Features.Products.Queries.GetProducts;

namespace SaasApi.API.Controllers
{
    public class SetProductStatusRequest
    {
        public bool IsActive { get; set; }
    }

    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ProductsController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] GetProductsQuery query, CancellationToken ct)
        {
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await mediator.Send(new GetProductByIdQuery(id), ct);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(CreateProduct), new { result.ProductId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        {
            UpdateProductCommand command = new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.Stock,
                request.Slug,
                request.ImageUrl,
                request.Sku);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "admin,super-admin")]
        public async Task<IActionResult> SetProductStatus([FromRoute] Guid id, [FromBody] SetProductStatusRequest request, CancellationToken ct)
        {
            await mediator.Send(new SetProductStatusCommand(id, request.IsActive), ct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,super-admin")]
        public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeleteProductCommand(id), ct);
            return NoContent();
        }




    }
}
