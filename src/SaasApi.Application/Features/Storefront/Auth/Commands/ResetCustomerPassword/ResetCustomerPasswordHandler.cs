using FluentValidation;
using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.ResetCustomerPassword;

public class ResetCustomerPasswordHandler(
    IRepository<CustomerPasswordResetToken> resetRepo,
    IRepository<Customer> customerRepo,
    IRepository<CustomerRefreshToken> refreshRepo,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetCustomerPasswordCommand>
{
    public async Task Handle(ResetCustomerPasswordCommand request, CancellationToken ct)
    {
        var tokens = await resetRepo.FindGlobalAsync(t => t.Token == request.Token, ct);
        if (!tokens.Any() || tokens.First().IsExpired)
            throw new NotFoundException("Reset token is invalid or has expired.");

        var resetToken = tokens.First();

        var customers = await customerRepo.FindGlobalAsync(c => c.Id == resetToken.CustomerId, ct);
        var customer = customers.First();

        if (passwordHasher.Verify(request.NewPassword, customer.PasswordHash))
            throw new ValidationException("New password must be different from your current password.");

        customer.ResetPassword(passwordHasher.Hash(request.NewPassword));

        var active = await refreshRepo.FindGlobalAsync(
            r => r.CustomerId == customer.Id && r.RevokedAt == null, ct);
        foreach (var t in active)
            t.Revoke();

        resetRepo.Remove(resetToken);
        await customerRepo.SaveChangesAsync(ct);
    }
}
