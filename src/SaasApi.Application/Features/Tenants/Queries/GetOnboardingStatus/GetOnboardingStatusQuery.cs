using MediatR;

namespace SaasApi.Application.Features.Tenants.Queries.GetOnboardingStatus;

public record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;
