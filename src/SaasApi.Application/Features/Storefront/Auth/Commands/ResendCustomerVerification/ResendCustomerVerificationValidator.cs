using FluentValidation;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ResendCustomerVerification;

public class ResendCustomerVerificationValidator : AbstractValidator<ResendCustomerVerificationCommand>
{
    public ResendCustomerVerificationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
