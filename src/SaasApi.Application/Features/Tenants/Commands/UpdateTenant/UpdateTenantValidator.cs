using FluentValidation;

namespace SaasApi.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
    {
        public UpdateTenantValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        }
    }
}
