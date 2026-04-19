using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ForgotCustomerPassword;

public record ForgotCustomerPasswordCommand(string Email) : IRequest<ForgotCustomerPasswordResult>;

public record ForgotCustomerPasswordResult(
    string? Email,
    string? ResetToken,
    string? StoreName,
    string? StoreUrl,
    string? CustomerFirstName);
