using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.Users.Commands.DeactivateUser;
using SaasApi.Application.Features.Users.Commands.UpdateMyProfile;
using SaasApi.Application.Features.Users.Commands.UpdateUserRole;
using SaasApi.Application.Features.Users.Queries.GetMyProfile;
using SaasApi.Application.Features.Users.Queries.GetUsers;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var result = await mediator.Send(new GetUsersQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyProfileQuery(), ct);
        return Ok(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateUserRoleCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.AdminAndAbove)]
    public async Task<IActionResult> DeactivateUser([FromRoute] Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }
}
