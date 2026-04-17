using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.DeactivateUser
{
    public class DeactivateUserHandler(
        IRepository<User> userRepo,
        IAuditService auditService) : IRequestHandler<DeactivateUserCommand>
    {
        public async Task Handle(DeactivateUserCommand request, CancellationToken ct)
        {
            var user = await userRepo.GetByIdAsync(request.Id, ct);

            if (user is null)
                throw new NotFoundException("User not found");

            user.Deactivate();
            userRepo.Update(user);
            await userRepo.SaveChangesAsync(ct);

            await auditService.LogAsync("user.deactivated", "User", user.Id, ct: ct);
        }
    }
}
