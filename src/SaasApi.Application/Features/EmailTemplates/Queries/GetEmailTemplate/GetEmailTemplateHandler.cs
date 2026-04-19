using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.EmailTemplates.Queries.GetEmailTemplate;

public class GetEmailTemplateHandler(
    IRepository<TenantEmailTemplate> templateRepo,
    IEmailTemplateRenderer renderer)
    : IRequestHandler<GetEmailTemplateQuery, EmailTemplateDetailDto>
{
    public async Task<EmailTemplateDetailDto> Handle(GetEmailTemplateQuery request, CancellationToken ct)
    {
        var matches = await templateRepo.FindAsync(t => t.Type == request.Type, ct);
        var custom = matches.FirstOrDefault();

        if (custom is not null)
        {
            return new EmailTemplateDetailDto(
                request.Type, custom.Subject, custom.BodyHtml, custom.Enabled,
                IsCustom: true,
                EmailTemplatePlaceholders.ForType(request.Type));
        }

        var def = renderer.GetDefault(request.Type);
        return new EmailTemplateDetailDto(
            request.Type, def.Subject, def.BodyHtml, def.Enabled,
            IsCustom: false,
            EmailTemplatePlaceholders.ForType(request.Type));
    }
}
