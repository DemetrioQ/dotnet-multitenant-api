using FluentValidation;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Users.Commands.UpdateUserRole
{
    public class UpdateUserRoleValidator : AbstractValidator<UpdateUserRoleCommand>
    {
        public UpdateUserRoleValidator()
        {
            // Merchants can only set Member or Admin. SuperAdmin is platform-internal and
            // not settable via this endpoint.
            RuleFor(x => x.Role)
                .Must(r => r is UserRole.Member or UserRole.Admin)
                .WithMessage("Role must be 'member' or 'admin'.");
        }
    }
}
