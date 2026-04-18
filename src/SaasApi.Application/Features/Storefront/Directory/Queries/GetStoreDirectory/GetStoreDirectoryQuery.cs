using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.Storefront.Directory.Queries.GetStoreDirectory;

public record GetStoreDirectoryQuery(int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<StoreDirectoryItemDto>>;
