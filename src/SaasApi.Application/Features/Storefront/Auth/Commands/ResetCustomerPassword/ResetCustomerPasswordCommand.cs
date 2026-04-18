using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ResetCustomerPassword;

public record ResetCustomerPasswordCommand(string Token, string NewPassword) : IRequest;
