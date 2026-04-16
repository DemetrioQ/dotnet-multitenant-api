using MediatR;

namespace SaasApi.Application.Features.Users.Commands.ResendVerification;

public record ResendVerificationCommand(string Slug, string Email) : IRequest<ResendVerificationResult>;

public record ResendVerificationResult(string? Token);
