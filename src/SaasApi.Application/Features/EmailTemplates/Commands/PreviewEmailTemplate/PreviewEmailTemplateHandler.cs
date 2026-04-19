using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.EmailTemplates.Commands.PreviewEmailTemplate;

public class PreviewEmailTemplateHandler(
    IEmailTemplateRenderer renderer,
    IRepository<Tenant> tenantRepo,
    ICurrentTenantService currentTenant,
    IStoreUrlBuilder storeUrlBuilder)
    : IRequestHandler<PreviewEmailTemplateCommand, EmailTemplatePreviewDto>
{
    public async Task<EmailTemplatePreviewDto> Handle(PreviewEmailTemplateCommand request, CancellationToken ct)
    {
        var tenant = await tenantRepo.GetByIdAsync(currentTenant.TenantId, ct);
        var storeName = tenant?.Name ?? "Your Store";
        var storeUrl = tenant is null ? "https://example.com" : storeUrlBuilder.BuildUrl(tenant.Slug);

        var sample = EmailTemplatePlaceholders.SampleModel(request.Type, storeName, storeUrl);
        var rendered = await renderer.RenderSourceAsync(request.Subject, request.BodyHtml, request.Enabled, sample, ct);
        return new EmailTemplatePreviewDto(rendered.Subject, rendered.HtmlBody, rendered.Enabled);
    }
}
