using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.LoginCustomer;

public record LoginCustomerCommand(string Email, string Password) : IRequest<LoginCustomerResult>;

public record LoginCustomerResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
