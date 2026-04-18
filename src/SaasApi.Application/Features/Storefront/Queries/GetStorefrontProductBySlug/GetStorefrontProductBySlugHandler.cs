using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Queries.GetStorefrontProductBySlug
{
    public class GetStorefrontProductBySlugHandler(IRepository<Product> productRepo)
        : IRequestHandler<GetStorefrontProductBySlugQuery, StorefrontProductDto>
    {
        public async Task<StorefrontProductDto> Handle(GetStorefrontProductBySlugQuery request, CancellationToken ct)
        {
            var matches = await productRepo.FindAsync(p => p.Slug == request.Slug && p.IsActive, ct);
            var product = matches.FirstOrDefault();
            if (product is null)
                throw new NotFoundException("Product not found.");

            return StorefrontProductDto.FromEntity(product);
        }
    }
}
