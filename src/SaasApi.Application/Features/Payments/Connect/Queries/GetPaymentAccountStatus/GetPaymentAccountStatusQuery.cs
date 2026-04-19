using MediatR;

namespace SaasApi.Application.Features.Payments.Connect.Queries.GetPaymentAccountStatus;

public record GetPaymentAccountStatusQuery : IRequest<PaymentAccountStatusDto>;
