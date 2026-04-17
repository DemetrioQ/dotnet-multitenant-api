using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.AcceptInvitation;

public class AcceptInvitationHandler(
    IRepository<Invitation> invitationRepo,
    IRepository<User> userRepo,
    IRepository<UserProfile> profileRepo,
    IRepository<TenantOnboardingStatus> onboardingRepo,
    IPasswordHasher passwordHasher
    ) : IRequestHandler<AcceptInvitationCommand>
{
    public async Task Handle(AcceptInvitationCommand request, CancellationToken ct)
    {
        var invitations = await invitationRepo.FindGlobalAsync(i => i.Token == request.Token, ct);
        var invitation = invitations.FirstOrDefault();

        if (invitation is null || invitation.IsAccepted || invitation.IsExpired)
            throw new BadRequestException("Invalid or expired invitation token.");

        // Edge case: user registered via open registration after the invite was sent
        var existingUsers = await userRepo.FindGlobalAsync(
            u => u.Email == invitation.Email && u.TenantId == invitation.TenantId, ct);
        if (existingUsers.Any())
            throw new ConflictException("A user with this email already exists in this tenant.");

        // First user in tenant becomes admin
        var tenantUsers = await userRepo.FindGlobalAsync(u => u.TenantId == invitation.TenantId, ct);
        var role = tenantUsers.Any() ? "member" : "admin";

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(invitation.TenantId, invitation.Email, passwordHash, role);
        user.VerifyEmail(); // email confirmed via invite link
        await userRepo.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id, user.TenantId, request.FirstName, request.LastName);
        await profileRepo.AddAsync(profile, ct);

        // Flip onboarding ProfileCompleted since firstName is provided
        var statuses = await onboardingRepo.FindGlobalAsync(s => s.TenantId == invitation.TenantId, ct);
        var status = statuses.FirstOrDefault();
        if (status is not null && !status.ProfileCompleted)
            status.CompleteProfile();

        invitation.Accept();
        await invitationRepo.SaveChangesAsync(ct);
    }
}
