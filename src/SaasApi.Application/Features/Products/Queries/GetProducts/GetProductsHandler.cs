using MediatR;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Queries.GetProducts
{
    public class GetProductsHandler(
        IRepository<Product> productRepo
        )
        : IRequestHandler<GetProductsQuery, GetProductsResult>
    {
        public async Task<GetProductsResult> Handle(GetProductsQuery request, CancellationToken ct)
        {
            int skip = (request.Page - 1) * request.PageSize;
            var products = await productRepo.GetPagedAsync(skip, request.PageSize, ct);
            int totalCount = await productRepo.CountAsync(ct);

            var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock)).ToList();

            return new GetProductsResult(dtos, totalCount);
        }
    }
}
