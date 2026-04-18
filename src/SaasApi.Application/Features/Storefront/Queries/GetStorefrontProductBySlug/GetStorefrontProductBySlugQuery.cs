using MediatR;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontProductBySlug
{
    public record GetStorefrontProductBySlugQuery(string Slug) : IRequest<StorefrontProductDto>;
}
