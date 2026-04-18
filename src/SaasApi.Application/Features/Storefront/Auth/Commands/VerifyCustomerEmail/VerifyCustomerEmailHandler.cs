using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Auth.Commands.VerifyCustomerEmail;

public class VerifyCustomerEmailHandler(
    IRepository<CustomerEmailVerificationToken> verificationRepo,
    IRepository<Customer> customerRepo) : IRequestHandler<VerifyCustomerEmailCommand>
{
    public async Task Handle(VerifyCustomerEmailCommand request, CancellationToken ct)
    {
        var tokens = await verificationRepo.FindGlobalAsync(t => t.Token == request.Token, ct);
        if (!tokens.Any() || tokens.First().IsExpired)
            throw new BadRequestException("Invalid or expired verification token.");

        var token = tokens.First();

        var customers = await customerRepo.FindGlobalAsync(c => c.Id == token.CustomerId, ct);
        var customer = customers.First();

        customer.VerifyEmail();
        verificationRepo.Remove(token);
        await customerRepo.SaveChangesAsync(ct);
    }
}
