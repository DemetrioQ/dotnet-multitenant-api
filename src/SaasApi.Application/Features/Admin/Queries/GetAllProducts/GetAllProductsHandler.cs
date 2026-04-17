using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Products.Queries;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Admin.Queries.GetAllProducts;

public class GetAllProductsHandler(IRepository<Product> productRepo)
    : IRequestHandler<GetAllProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken ct)
    {
        var all = await productRepo.FindGlobalAsync(_ => true, ct);
        var totalCount = all.Count;

        var paged = all
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = paged.Select(ProductDto.FromEntity).ToList();

        return new PagedResult<ProductDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
