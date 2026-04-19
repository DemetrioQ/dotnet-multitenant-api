using MediatR;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.EmailTemplates.Commands.DeleteEmailTemplate;

public record DeleteEmailTemplateCommand(EmailTemplateType Type) : IRequest;
