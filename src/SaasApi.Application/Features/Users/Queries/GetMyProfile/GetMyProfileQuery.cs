using MediatR;

namespace SaasApi.Application.Features.Users.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<UserProfileDto>;
