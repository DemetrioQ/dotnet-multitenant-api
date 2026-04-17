using FluentValidation;

namespace SaasApi.Application.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.AvatarUrl).MaximumLength(500).When(x => x.AvatarUrl is not null);
        RuleFor(x => x.Bio).MaximumLength(500).When(x => x.Bio is not null);
    }
}
