using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Users.Commands.AcceptInvitation;
using SaasApi.Application.Features.Users.Commands.InviteUser;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class InvitationsController(
    IMediator mediator,
    IBackgroundJobQueue jobQueue,
    IConfiguration config) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> Invite([FromBody] InviteUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        if (result.Token is not null)
        {
            var frontendUrl = config["App:FrontendUrl"] ?? "http://localhost:5173";
            var inviteLink = $"{frontendUrl}/accept-invitation?token={result.Token}";
            var email = command.Email;
            await jobQueue.EnqueueAsync(async (sp, ct) =>
            {
                var emailService = sp.GetRequiredService<IEmailService>();
                await emailService.SendInvitationEmailAsync(email, inviteLink, ct);
            }, ct);
        }

        return Ok(new { message = "If the email is not already registered, an invitation has been sent." });
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(new { message = "Invitation accepted. You can now log in." });
    }
}
