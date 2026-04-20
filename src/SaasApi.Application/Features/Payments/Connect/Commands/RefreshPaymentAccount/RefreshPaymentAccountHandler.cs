using MediatR;
using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Payments.Connect.Commands.RefreshPaymentAccount;

public class RefreshPaymentAccountHandler(
    IRepository<TenantPaymentAccount> accountRepo,
    IPaymentService paymentService,
    IAuditService auditService,
    IConfiguration config)
    : IRequestHandler<RefreshPaymentAccountCommand, PaymentAccountStatusDto>
{
    public async Task<PaymentAccountStatusDto> Handle(RefreshPaymentAccountCommand request, CancellationToken ct)
    {
        var accounts = await accountRepo.FindAsync(_ => true, ct);
        var account = accounts.FirstOrDefault()
                      ?? throw new NotFoundException("No payment account connected for this tenant yet.");

        var info = await paymentService.RefreshAccountStatusAsync(account.AccountId, ct);
        var wasComplete = account.CanAcceptPayments;
        account.SyncStatus(info.ChargesEnabled, info.DetailsSubmitted);

        accountRepo.Update(account);
        await accountRepo.SaveChangesAsync(ct);

        if (!wasComplete && account.CanAcceptPayments)
        {
            await auditService.LogAsync(
                "payments.account_ready",
                "TenantPaymentAccount",
                account.Id,
                $"Connected {account.Provider} account {account.AccountId} completed onboarding.",
                ct);
        }

        var feePercent = config.GetValue<decimal?>("Payments:PlatformFeePercent") ?? 0.05m;
        return PaymentAccountStatusDto.FromEntity(account, feePercent);
    }
}
