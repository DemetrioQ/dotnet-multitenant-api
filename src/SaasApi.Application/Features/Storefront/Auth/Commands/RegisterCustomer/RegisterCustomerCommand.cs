using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.RegisterCustomer;

public record RegisterCustomerCommand(string Email, string Password, string FirstName, string LastName)
    : IRequest<RegisterCustomerResult>;

public record RegisterCustomerResult(Guid CustomerId, string EmailVerificationToken);
