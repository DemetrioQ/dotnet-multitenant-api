using FluentValidation;

namespace SaasApi.Application.Features.EmailTemplates.Commands.UpsertEmailTemplate;

public class UpsertEmailTemplateValidator : AbstractValidator<UpsertEmailTemplateCommand>
{
    public UpsertEmailTemplateValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyHtml).NotEmpty().MaximumLength(20_000);
    }
}
