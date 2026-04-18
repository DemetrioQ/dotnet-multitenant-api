using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ResendCustomerVerification;

public record ResendCustomerVerificationCommand(string Email) : IRequest<ResendCustomerVerificationResult>;

public record ResendCustomerVerificationResult(string? Token, string? StoreName);
