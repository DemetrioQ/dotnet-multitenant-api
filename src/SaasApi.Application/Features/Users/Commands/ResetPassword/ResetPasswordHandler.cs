using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.ResetPassword;

public class ResetPasswordHandler(
    IRepository<PasswordResetToken> resetTokenRepo,
    IRepository<User> userRepo,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokens = await resetTokenRepo.FindGlobalAsync(t => t.Token == request.Token, ct);

        if (!tokens.Any() || tokens.First().IsExpired)
            throw new NotFoundException("Reset token is invalid or has expired.");

        var resetToken = tokens.First();

        var users = await userRepo.FindGlobalAsync(u => u.Id == resetToken.UserId, ct);
        var user = users.First();

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.ResetPassword(newHash);
        resetTokenRepo.Remove(resetToken);
        await userRepo.SaveChangesAsync(ct);
    }
}
