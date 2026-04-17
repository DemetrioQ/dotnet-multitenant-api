using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.InviteUser;

public class InviteUserHandler(
    IRepository<User> userRepo,
    IRepository<Invitation> invitationRepo,
    ICurrentTenantService currentTenantService,
    IAuditService auditService
    ) : IRequestHandler<InviteUserCommand, InviteUserResult>
{
    public async Task<InviteUserResult> Handle(InviteUserCommand request, CancellationToken ct)
    {
        // Anti-enumeration: silently do nothing if email is already a user in this tenant
        var existingUsers = await userRepo.FindAsync(u => u.Email == request.Email, ct);
        if (existingUsers.Any())
            return new InviteUserResult(null);

        // Replace any existing pending invitation for this email in this tenant
        var existingInvites = await invitationRepo.FindAsync(
            i => i.Email == request.Email && i.AcceptedAt == null, ct);
        foreach (var old in existingInvites)
            invitationRepo.Remove(old);

        var invitation = Invitation.Create(currentTenantService.TenantId, request.Email);
        await invitationRepo.AddAsync(invitation, ct);
        await invitationRepo.SaveChangesAsync(ct);

        await auditService.LogAsync("user.invited", "Invitation", invitation.Id, $"Invited {request.Email}", ct);

        return new InviteUserResult(invitation.Token);
    }
}
