using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Products.Commands.CreateProduct;
using SaasApi.Application.Features.Products.Commands.DeleteProduct;
using SaasApi.Application.Features.Products.Commands.UpdateProduct;
using SaasApi.Application.Features.Products.Queries.GetProductById;
using SaasApi.Application.Features.Products.Queries.GetProducts;

namespace SaasApi.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
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
            UpdateProductCommand command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.Stock);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken ct)
        {
            await mediator.Send(new DeleteProductCommand(id), ct);
            return NoContent();
        }




    }
}
