using FluentValidation;

namespace SaasApi.Application.Features.Tenants.Commands.CreateTenant
{
    public class CreateTenantValidator :  AbstractValidator<CreateTenantCommand>
    {
        public CreateTenantValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
        }
    }
}
