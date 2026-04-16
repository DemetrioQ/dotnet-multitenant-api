using FluentValidation;

namespace SaasApi.Application.Features.Users.Commands.ResendVerification;

public class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationValidator()
    {
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
