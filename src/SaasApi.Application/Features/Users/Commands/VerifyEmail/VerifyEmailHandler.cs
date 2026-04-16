using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.VerifyEmail;

public class VerifyEmailHandler(
    IRepository<EmailVerificationToken> verificationTokenRepo,
    IRepository<User> userRepo) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var tokens = await verificationTokenRepo.FindGlobalAsync(
            t => t.Token == request.Token, ct);

        if (!tokens.Any() || tokens.First().IsExpired)
            throw new BadRequestException("Invalid or expired verification token.");

        var verificationToken = tokens.First();

        var users = await userRepo.FindGlobalAsync(u => u.Id == verificationToken.UserId, ct);
        var user = users.First();

        user.VerifyEmail();
        verificationTokenRepo.Remove(verificationToken);
        await userRepo.SaveChangesAsync(ct);
    }
}
