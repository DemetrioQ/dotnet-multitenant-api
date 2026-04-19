using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.EmailTemplates.Queries.ListEmailTemplates;

public class ListEmailTemplatesHandler(
    IRepository<TenantEmailTemplate> templateRepo,
    IEmailTemplateRenderer renderer)
    : IRequestHandler<ListEmailTemplatesQuery, IReadOnlyList<EmailTemplateListItemDto>>
{
    public async Task<IReadOnlyList<EmailTemplateListItemDto>> Handle(ListEmailTemplatesQuery request, CancellationToken ct)
    {
        var overrides = await templateRepo.FindAsync(_ => true, ct);
        var overridesByType = overrides.ToDictionary(o => o.Type);

        var types = Enum.GetValues<EmailTemplateType>();
        var items = new List<EmailTemplateListItemDto>(types.Length);
        foreach (var type in types)
        {
            var def = renderer.GetDefault(type);
            overridesByType.TryGetValue(type, out var custom);

            items.Add(new EmailTemplateListItemDto(
                type,
                def.Subject,
                def.BodyHtml,
                def.Enabled,
                custom?.Subject,
                custom?.BodyHtml,
                custom?.Enabled,
                custom?.UpdatedAt ?? custom?.CreatedAt,
                EmailTemplatePlaceholders.ForType(type)));
        }

        return items;
    }
}
