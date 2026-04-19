using MediatR;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.EmailTemplates.Commands.PreviewEmailTemplate;

public record PreviewEmailTemplateCommand(
    EmailTemplateType Type,
    string Subject,
    string BodyHtml,
    bool Enabled) : IRequest<EmailTemplatePreviewDto>;
