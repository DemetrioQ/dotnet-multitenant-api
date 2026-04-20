using MediatR;
using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Payments.Connect.Queries.GetPaymentAccountStatus;

public class GetPaymentAccountStatusHandler(
    IRepository<TenantPaymentAccount> accountRepo,
    IPaymentService paymentService,
    IConfiguration config)
    : IRequestHandler<GetPaymentAccountStatusQuery, PaymentAccountStatusDto>
{
    public async Task<PaymentAccountStatusDto> Handle(GetPaymentAccountStatusQuery request, CancellationToken ct)
    {
        var accounts = await accountRepo.FindAsync(_ => true, ct);
        var account = accounts.FirstOrDefault();
        var feePercent = config.GetValue<decimal?>("Payments:PlatformFeePercent") ?? 0.05m;

        if (account is null)
            return PaymentAccountStatusDto.NotConnected(paymentService.ProviderName, feePercent);

        return PaymentAccountStatusDto.FromEntity(account, feePercent);
    }
}
