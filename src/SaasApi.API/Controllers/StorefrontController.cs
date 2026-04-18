using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Storefront.Queries.GetStorefrontInfo;
using SaasApi.Application.Features.Storefront.Queries.GetStorefrontProductBySlug;
using SaasApi.Application.Features.Storefront.Queries.GetStorefrontProducts;

namespace SaasApi.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [AllowAnonymous]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class StorefrontController(
        IMediator mediator,
        ICurrentTenantService currentTenantService) : ControllerBase
    {
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] GetStorefrontProductsQuery query, CancellationToken ct)
        {
            EnsureTenantResolved();
            var result = await mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("products/{slug}")]
        public async Task<IActionResult> GetProductBySlug([FromRoute] string slug, CancellationToken ct)
        {
            EnsureTenantResolved();
            var result = await mediator.Send(new GetStorefrontProductBySlugQuery(slug), ct);
            return Ok(result);
        }

        [HttpGet("store")]
        public async Task<IActionResult> GetStore(CancellationToken ct)
        {
            EnsureTenantResolved();
            var result = await mediator.Send(new GetStorefrontInfoQuery(), ct);
            return Ok(result);
        }

        private void EnsureTenantResolved()
        {
            if (!currentTenantService.IsResolved)
                throw new NotFoundException("Store not found.");
        }
    }
}
