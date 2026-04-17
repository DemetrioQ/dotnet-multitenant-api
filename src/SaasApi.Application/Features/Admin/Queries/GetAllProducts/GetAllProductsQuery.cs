using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.Products.Queries;

namespace SaasApi.Application.Features.Admin.Queries.GetAllProducts;

public record GetAllProductsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
