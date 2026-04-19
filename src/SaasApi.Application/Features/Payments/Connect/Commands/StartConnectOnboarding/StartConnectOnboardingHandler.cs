using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Payments.Connect.Commands.StartConnectOnboarding;

public class StartConnectOnboardingHandler(
    IRepository<TenantPaymentAccount> accountRepo,
    IRepository<Tenant> tenantRepo,
    IRepository<TenantSettings> settingsRepo,
    ICurrentTenantService currentTenant,
    ICurrentUserService currentUser,
    IPaymentService paymentService,
    IAuditService auditService)
    : IRequestHandler<StartConnectOnboardingCommand, StartOnboardingResult>
{
    public async Task<StartOnboardingResult> Handle(StartConnectOnboardingCommand request, CancellationToken ct)
    {
        var tenant = await tenantRepo.GetByIdAsync(currentTenant.TenantId, ct)
                     ?? throw new NotFoundException("Tenant not found.");

        // Pick an email to associate with the Stripe account — prefer the tenant's
        // configured support email, fall back to the merchant-admin who clicked the
        // button. Stripe uses this as the login email on their side.
        var settings = (await settingsRepo.FindAsync(_ => true, ct)).FirstOrDefault();
        var email = !string.IsNullOrWhiteSpace(settings?.SupportEmail)
            ? settings!.SupportEmail!
            : currentUser.Email ?? $"{tenant.Slug}@{tenant.Slug}.shop.demetrioq.com";

        // If we already have a payment account record, reuse it (merchant may be
        // resuming onboarding they started earlier).
        var accounts = await accountRepo.FindAsync(_ => true, ct);
        var account = accounts.FirstOrDefault();

        if (account is null)
        {
            var created = await paymentService.CreateConnectAccountAsync(tenant.Id, tenant.Name, email, ct);
            account = TenantPaymentAccount.Create(tenant.Id, created.Provider, created.AccountId);
            await accountRepo.AddAsync(account, ct);
            await accountRepo.SaveChangesAsync(ct);

            await auditService.LogAsync(
                "payments.account_created",
                "TenantPaymentAccount",
                account.Id,
                $"Connected {created.Provider} account {created.AccountId} created.",
                ct);
        }

        var link = await paymentService.CreateOnboardingLinkAsync(
            account.AccountId, request.RefreshUrl, request.ReturnUrl, ct);

        return new StartOnboardingResult(link.Url, link.ExpiresAt);
    }
}
