using MediatR;

namespace SaasApi.Application.Features.Users.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest;
