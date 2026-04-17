using FluentValidation;

namespace SaasApi.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
    {
        public UpdateTenantValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Length(3)
                .Matches("^[A-Z]{3}$").WithMessage("Currency must be a 3-letter ISO code (e.g. USD).");

            RuleFor(x => x.SupportEmail)
                .EmailAddress().When(x => x.SupportEmail is not null)
                .MaximumLength(320);

            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(500)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => x.WebsiteUrl is not null)
                .WithMessage("WebsiteUrl must be a valid URL.");
        }
    }
}
