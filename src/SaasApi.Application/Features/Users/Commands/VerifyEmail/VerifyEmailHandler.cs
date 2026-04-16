using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.VerifyEmail;

public class VerifyEmailHandler(IRepository<User> userRepo) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var users = await userRepo.FindGlobalAsync(
            u => u.EmailVerificationToken == request.Token, ct);

        if (!users.Any())
            throw new BadRequestException("Invalid or expired verification token.");

        var user = users.First();

        if (user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Verification token has expired. Please register again.");

        user.VerifyEmail();
        await userRepo.SaveChangesAsync(ct);
    }
}
