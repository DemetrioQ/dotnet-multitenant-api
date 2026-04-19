using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Payments.Connect.Queries.GetPaymentAccountStatus;

public class GetPaymentAccountStatusHandler(
    IRepository<TenantPaymentAccount> accountRepo,
    IPaymentService paymentService)
    : IRequestHandler<GetPaymentAccountStatusQuery, PaymentAccountStatusDto>
{
    public async Task<PaymentAccountStatusDto> Handle(GetPaymentAccountStatusQuery request, CancellationToken ct)
    {
        var accounts = await accountRepo.FindAsync(_ => true, ct);
        var account = accounts.FirstOrDefault();

        if (account is null)
            return PaymentAccountStatusDto.NotConnected(paymentService.ProviderName);

        return PaymentAccountStatusDto.FromEntity(account);
    }
}
