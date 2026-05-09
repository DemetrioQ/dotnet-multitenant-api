using FluentValidation;
using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Demo.Commands.ElevateDemoRole;

public class ElevateDemoRoleValidator : AbstractValidator<ElevateDemoRoleCommand>
{
    private static readonly string[] AllowedRoles =
    [
        RoleNames.Member, RoleNames.Admin, RoleNames.SuperAdmin
    ];

    public ElevateDemoRoleValidator()
    {
        RuleFor(c => c.Role)
            .NotEmpty()
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}
