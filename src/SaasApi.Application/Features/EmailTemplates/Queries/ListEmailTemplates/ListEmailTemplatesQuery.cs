using MediatR;

namespace SaasApi.Application.Features.EmailTemplates.Queries.ListEmailTemplates;

public record ListEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplateListItemDto>>;
