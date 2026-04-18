using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.LoginCustomer;

public class LoginCustomerHandler(
    IRepository<Customer> customerRepo,
    IRepository<CustomerRefreshToken> refreshRepo,
    IRepository<CustomerEmailVerificationToken> verificationRepo,
    ICurrentTenantService currentTenantService,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCustomerCommand, LoginCustomerResult>
{
    public async Task<LoginCustomerResult> Handle(LoginCustomerCommand request, CancellationToken ct)
    {
        if (!currentTenantService.IsResolved)
            throw new UnauthorizedAccessException("Invalid credentials");

        var matches = await customerRepo.FindAsync(c => c.Email == request.Email, ct);
        if (!matches.Any())
            throw new UnauthorizedAccessException("Invalid credentials");

        var customer = matches.First();

        if (!customer.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!passwordHasher.Verify(request.Password, customer.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!customer.IsEmailVerified)
        {
            var tokens = await verificationRepo.FindAsync(t => t.CustomerId == customer.Id, ct);
            var existing = tokens.FirstOrDefault();
            var canResendAt = existing is not null
                ? existing.CreatedAt.AddMinutes(2)
                : DateTime.UtcNow;
            throw new EmailNotVerifiedException(canResendAt);
        }

        var refresh = CustomerRefreshToken.Create(customer.TenantId, customer.Id, Guid.NewGuid());
        await refreshRepo.AddAsync(refresh, ct);
        await refreshRepo.SaveChangesAsync(ct);

        var jwt = jwtTokenService.GenerateToken(customer);
        return new LoginCustomerResult(jwt, refresh.Token, refresh.ExpiresAt);
    }
}
