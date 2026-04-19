using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasApi.Application.Features.EmailTemplates.Commands.DeleteEmailTemplate;
using SaasApi.Application.Features.EmailTemplates.Commands.PreviewEmailTemplate;
using SaasApi.Application.Features.EmailTemplates.Commands.UpsertEmailTemplate;
using SaasApi.Application.Features.EmailTemplates.Queries.GetEmailTemplate;
using SaasApi.Application.Features.EmailTemplates.Queries.ListEmailTemplates;
using SaasApi.Domain.Entities;

namespace SaasApi.API.Controllers;

public class UpsertEmailTemplateRequest
{
    public string Subject { get; set; } = default!;
    public string BodyHtml { get; set; } = default!;
    public bool Enabled { get; set; } = true;
}

public class PreviewEmailTemplateRequest
{
    public string Subject { get; set; } = default!;
    public string BodyHtml { get; set; } = default!;
    public bool Enabled { get; set; } = true;
}

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = RoleNames.AdminAndAbove)]
[Route("api/v{version:apiVersion}/email-templates")]
public class EmailTemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await mediator.Send(new ListEmailTemplatesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{type}")]
    public async Task<IActionResult> Get([FromRoute] EmailTemplateType type, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEmailTemplateQuery(type), ct);
        return Ok(result);
    }

    [HttpPut("{type}")]
    public async Task<IActionResult> Upsert([FromRoute] EmailTemplateType type, [FromBody] UpsertEmailTemplateRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new UpsertEmailTemplateCommand(type, body.Subject, body.BodyHtml, body.Enabled), ct);
        return Ok(result);
    }

    [HttpDelete("{type}")]
    public async Task<IActionResult> RevertToDefault([FromRoute] EmailTemplateType type, CancellationToken ct)
    {
        await mediator.Send(new DeleteEmailTemplateCommand(type), ct);
        return NoContent();
    }

    [HttpPost("{type}/preview")]
    public async Task<IActionResult> Preview([FromRoute] EmailTemplateType type, [FromBody] PreviewEmailTemplateRequest body, CancellationToken ct)
    {
        var result = await mediator.Send(new PreviewEmailTemplateCommand(type, body.Subject, body.BodyHtml, body.Enabled), ct);
        return Ok(result);
    }
}
