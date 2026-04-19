using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Payments.Connect.Commands.RefreshPaymentAccount;
using SaasApi.Application.Features.Payments.Connect.Commands.StartConnectOnboarding;
using SaasApi.Application.Features.Payments.Connect.Queries.GetPaymentAccountStatus;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = RoleNames.AdminAndAbove)]
[Route("api/v{version:apiVersion}/payments/connect")]
public class PaymentsConnectController(IMediator mediator) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPaymentAccountStatusQuery(), ct);
        return Ok(result);
    }

    [HttpPost("onboarding")]
    public async Task<IActionResult> StartOnboarding([FromBody] StartConnectOnboardingCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("refresh-status")]
    public async Task<IActionResult> RefreshStatus(CancellationToken ct)
    {
        var result = await mediator.Send(new RefreshPaymentAccountCommand(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Dev-only endpoint used by the simulation payment provider. The fake onboarding URL
    /// returned by <see cref="Services.Payments.SimulationPaymentService"/> points here;
    /// when the merchant hits it (via browser redirect from the dashboard), we auto-mark
    /// the account as complete and bounce them back to their returnUrl — mimicking
    /// Stripe's hosted onboarding without any real account setup.
    /// </summary>
    [HttpGet("simulate-onboarding")]
    [AllowAnonymous]
    public async Task<IActionResult> SimulateOnboarding(
        [FromQuery] string accountId,
        [FromQuery] string returnUrl,
        CancellationToken ct)
    {
        var payload = $"{{\"id\":\"evt_{Guid.NewGuid():N}\",\"type\":\"account.updated\"," +
                      $"\"accountId\":\"{accountId}\",\"chargesEnabled\":true,\"detailsSubmitted\":true}}";
        await mediator.Send(
            new SaasApi.Application.Features.Storefront.Orders.Commands.HandlePaymentWebhook.HandlePaymentWebhookCommand(payload, null),
            ct);
        return Redirect(returnUrl);
    }
}
