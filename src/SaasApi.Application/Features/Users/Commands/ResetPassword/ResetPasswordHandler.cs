using FluentValidation;
using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.ResetPassword;

public class ResetPasswordHandler(
    IRepository<PasswordResetToken> resetTokenRepo,
    IRepository<User> userRepo,
    IRepository<RefreshToken> refreshTokenRepo,
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

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationException("New password must be different from your current password.");

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.ResetPassword(newHash);

        var activeTokens = await refreshTokenRepo.FindGlobalAsync(
            r => r.UserId == user.Id && r.RevokedAt == null, ct);
        foreach (var t in activeTokens)
            t.Revoke();

        resetTokenRepo.Remove(resetToken);
        await userRepo.SaveChangesAsync(ct);
    }
}
