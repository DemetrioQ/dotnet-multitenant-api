using MediatR;

namespace SaasApi.Application.Features.Users.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    string? Bio) : IRequest<Unit>;
