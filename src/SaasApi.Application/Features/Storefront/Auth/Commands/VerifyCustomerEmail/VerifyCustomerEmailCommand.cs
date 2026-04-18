using MediatR;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.VerifyCustomerEmail;

public record VerifyCustomerEmailCommand(string Token) : IRequest;
