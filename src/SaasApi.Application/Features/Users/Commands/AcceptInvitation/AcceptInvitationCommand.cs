using MediatR;

namespace SaasApi.Application.Features.Users.Commands.AcceptInvitation;

public record AcceptInvitationCommand(string Token, string Password, string FirstName, string LastName) : IRequest;
