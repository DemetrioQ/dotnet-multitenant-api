using MediatR;

namespace SaasApi.Application.Features.Storefront.Orders.Commands.HandlePaymentWebhook;

public record HandlePaymentWebhookCommand(string Payload, string? SignatureHeader) : IRequest;
