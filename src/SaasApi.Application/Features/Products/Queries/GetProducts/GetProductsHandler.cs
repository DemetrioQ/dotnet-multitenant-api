using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Queries.GetProducts
{
    public class GetProductsHandler(IRepository<Product> productRepo)
        : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
    {
        public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
        {
            int skip = (request.Page - 1) * request.PageSize;
            var products = await productRepo.GetPagedAsync(skip, request.PageSize, ct);
            int totalCount = await productRepo.CountAsync(ct);

            var dtos = products.Select(ProductDto.FromEntity).ToList();

            return new PagedResult<ProductDto>(dtos, totalCount, request.Page, request.PageSize);
        }
    }
}
