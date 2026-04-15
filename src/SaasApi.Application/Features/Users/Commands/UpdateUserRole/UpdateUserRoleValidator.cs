using FluentValidation;

namespace SaasApi.Application.Features.Users.Commands.UpdateUserRole
{
    public class UpdateUserRoleValidator : AbstractValidator<UpdateUserRoleCommand>
    {
        private static readonly string[] AllowedRoles = ["admin", "member"];

        public UpdateUserRoleValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(r => AllowedRoles.Contains(r))
                .WithMessage("Role must be 'admin' or 'member'.");
        }
    }
}
