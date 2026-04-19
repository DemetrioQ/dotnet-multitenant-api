using MediatR;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.EmailTemplates.Commands.UpsertEmailTemplate;

public record UpsertEmailTemplateCommand(
    EmailTemplateType Type,
    string Subject,
    string BodyHtml,
    bool Enabled) : IRequest<EmailTemplateDetailDto>;
