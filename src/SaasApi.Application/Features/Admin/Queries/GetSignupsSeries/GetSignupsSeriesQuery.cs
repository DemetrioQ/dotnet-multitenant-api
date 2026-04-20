using MediatR;

namespace SaasApi.Application.Features.Admin.Queries.GetSignupsSeries;

public enum SignupEntity
{
    Tenants = 0,
    Users = 1,
    Customers = 2
}

public record GetSignupsSeriesQuery(SignupEntity Entity, int Days) : IRequest<SignupsSeriesDto>;

public record SignupPointDto(DateOnly Date, int Count);

public record SignupsSeriesDto(SignupEntity Entity, int Days, IReadOnlyList<SignupPointDto> Points);
