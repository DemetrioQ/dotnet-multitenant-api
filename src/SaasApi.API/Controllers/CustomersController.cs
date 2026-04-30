using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.API.Authorization;
using SaasApi.Application.Features.MerchantCustomers.Queries.GetMerchantCustomerById;
using SaasApi.Application.Features.MerchantCustomers.Queries.GetMerchantCustomers;
using SaasApi.Domain.Common;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/customers")]
public class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequireScope(OAuthScopes.CustomersRead)]
    public async Task<IActionResult> GetCustomers([FromQuery] GetMerchantCustomersQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequireScope(OAuthScopes.CustomersRead)]
    public async Task<IActionResult> GetCustomerById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetMerchantCustomerByIdQuery(id), ct);
        return Ok(result);
    }
}
