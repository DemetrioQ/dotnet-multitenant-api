using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Application.Features.Users.Queries;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Users.Commands.UpdateUserRole
{
    public class UpdateUserRoleHandler(
        IRepository<User> userRepo,
        IAuditService auditService) : IRequestHandler<UpdateUserRoleCommand, UserDto>
    {
        public async Task<UserDto> Handle(UpdateUserRoleCommand request, CancellationToken ct)
        {
            var user = await userRepo.GetByIdAsync(request.Id, ct);

            if (user is null)
                throw new NotFoundException("User not found");

            user.UpdateRole(request.Role);
            userRepo.Update(user);
            await userRepo.SaveChangesAsync(ct);

            await auditService.LogAsync("user.role_updated", "User", user.Id, $"Role changed to {request.Role.ToDbString()}", ct);

            return UserDto.FromEntity(user);
        }
    }
}
