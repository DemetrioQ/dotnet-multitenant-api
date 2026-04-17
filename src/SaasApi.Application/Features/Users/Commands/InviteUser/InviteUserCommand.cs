using MediatR;

namespace SaasApi.Application.Features.Users.Commands.InviteUser;

public record InviteUserCommand(string Email) : IRequest<InviteUserResult>;

// Token is null when silently skipped (anti-enumeration)
public record InviteUserResult(string? Token);
