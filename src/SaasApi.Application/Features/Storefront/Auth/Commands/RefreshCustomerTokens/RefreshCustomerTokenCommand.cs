using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.RefreshCustomerTokens;

public record RefreshCustomerTokenCommand(string RefreshToken) : IRequest<RefreshCustomerTokenResult>;

public record RefreshCustomerTokenResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
