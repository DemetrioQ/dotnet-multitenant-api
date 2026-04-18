using System.Text;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Storefront.Orders.Commands.HandlePaymentWebhook;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/storefront/payments")]
public class PaymentWebhookController(IMediator mediator) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers.TryGetValue("Stripe-Signature", out var sig)
            ? sig.ToString()
            : null;

        try
        {
            await mediator.Send(new HandlePaymentWebhookCommand(payload, signature), ct);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        return Ok();
    }

    /// <summary>
    /// Dev-only simulation endpoint. When Payments:Provider=simulation, the payment URL
    /// returned from CreateCheckoutSession points here. Customer hits it to mark the session
    /// completed (mimicking what Stripe's hosted checkout would do).
    /// </summary>
    [HttpGet("simulate")]
    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate([FromQuery] string sessionId, [FromQuery] string outcome = "completed", CancellationToken ct = default)
    {
        var type = outcome == "expired" ? "session.expired" : "session.completed";
        var payload = $"{{\"id\":\"evt_{Guid.NewGuid():N}\",\"type\":\"{type}\",\"sessionId\":\"{sessionId}\"}}";
        await mediator.Send(new HandlePaymentWebhookCommand(payload, null), ct);
        return Ok(new { message = $"Simulated {type} for session {sessionId}." });
    }
}
