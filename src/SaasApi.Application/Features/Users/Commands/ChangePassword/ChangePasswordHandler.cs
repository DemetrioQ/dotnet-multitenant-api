using FluentValidation;
using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.ChangePassword;

public class ChangePasswordHandler(
    IRepository<User> userRepo,
    IRepository<RefreshToken> refreshTokenRepo,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Not authenticated.");

        var users = await userRepo.FindGlobalAsync(u => u.Id == currentUser.UserId, ct);
        if (!users.Any())
            throw new NotFoundException("User not found.");

        var user = users.First();

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("Current password is incorrect.");

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationException("New password must be different from your current password.");

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.ResetPassword(newHash);

        var activeTokens = await refreshTokenRepo.FindGlobalAsync(
            r => r.UserId == user.Id && r.RevokedAt == null, ct);
        foreach (var t in activeTokens)
            t.Revoke();

        await userRepo.SaveChangesAsync(ct);

        await auditService.LogAsync("user.password_changed", "User", user.Id, null, ct);
    }
}
