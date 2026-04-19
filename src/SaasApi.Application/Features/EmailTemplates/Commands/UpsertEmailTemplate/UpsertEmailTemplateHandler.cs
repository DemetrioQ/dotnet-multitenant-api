using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.EmailTemplates.Commands.UpsertEmailTemplate;

public class UpsertEmailTemplateHandler(
    IRepository<TenantEmailTemplate> templateRepo,
    ICurrentTenantService currentTenant,
    IAuditService auditService)
    : IRequestHandler<UpsertEmailTemplateCommand, EmailTemplateDetailDto>
{
    public async Task<EmailTemplateDetailDto> Handle(UpsertEmailTemplateCommand request, CancellationToken ct)
    {
        var matches = await templateRepo.FindAsync(t => t.Type == request.Type, ct);
        var existing = matches.FirstOrDefault();

        if (existing is null)
        {
            var created = TenantEmailTemplate.Create(
                currentTenant.TenantId, request.Type, request.Subject, request.BodyHtml, request.Enabled);
            await templateRepo.AddAsync(created, ct);
            await templateRepo.SaveChangesAsync(ct);

            await auditService.LogAsync("email_template.created", "TenantEmailTemplate", created.Id, request.Type.ToString(), ct);

            return new EmailTemplateDetailDto(
                request.Type, created.Subject, created.BodyHtml, created.Enabled, IsCustom: true,
                EmailTemplatePlaceholders.ForType(request.Type));
        }

        existing.Update(request.Subject, request.BodyHtml, request.Enabled);
        templateRepo.Update(existing);
        await templateRepo.SaveChangesAsync(ct);

        await auditService.LogAsync("email_template.updated", "TenantEmailTemplate", existing.Id, request.Type.ToString(), ct);

        return new EmailTemplateDetailDto(
            request.Type, existing.Subject, existing.BodyHtml, existing.Enabled, IsCustom: true,
            EmailTemplatePlaceholders.ForType(request.Type));
    }
}
