using MediatR;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontInfo
{
    public record GetStorefrontInfoQuery : IRequest<StorefrontInfoDto>;
}
