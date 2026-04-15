using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Users.Commands.DeactivateUser;
using SaasApi.Application.Features.Users.Commands.UpdateUserRole;
using SaasApi.Application.Features.Users.Queries.GetUsers;

namespace SaasApi.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUsersQuery(), ct);
        return Ok(result);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateUserRoleCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }
}
