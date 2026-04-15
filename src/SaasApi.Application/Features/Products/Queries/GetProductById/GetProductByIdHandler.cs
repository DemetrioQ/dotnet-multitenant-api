using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Queries.GetProductById
{
    public class GetProductByIdHandler(
        IRepository<Product> productRepo
        ) : IRequestHandler<GetProductByIdQuery, ProductDto>
    {
        public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
        {
            var product = await productRepo.GetByIdAsync(request.Id, ct);

            if (product is null)
                throw new NotFoundException("Product not found");

            return ProductDto.FromEntity(product);
        }
    }
}
