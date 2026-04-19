using MediatR;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.EmailTemplates.Queries.GetEmailTemplate;

public record GetEmailTemplateQuery(EmailTemplateType Type) : IRequest<EmailTemplateDetailDto>;
