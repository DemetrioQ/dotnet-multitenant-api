using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.EmailTemplates.Commands.DeleteEmailTemplate;

public class DeleteEmailTemplateHandler(
    IRepository<TenantEmailTemplate> templateRepo,
    IAuditService auditService)
    : IRequestHandler<DeleteEmailTemplateCommand>
{
    public async Task Handle(DeleteEmailTemplateCommand request, CancellationToken ct)
    {
        var matches = await templateRepo.FindAsync(t => t.Type == request.Type, ct);
        var existing = matches.FirstOrDefault();
        if (existing is null) return;

        templateRepo.Remove(existing);
        await templateRepo.SaveChangesAsync(ct);

        await auditService.LogAsync("email_template.reverted", "TenantEmailTemplate", existing.Id, request.Type.ToString(), ct);
    }
}
