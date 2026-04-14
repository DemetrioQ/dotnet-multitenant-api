using MediatR;
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
                throw new KeyNotFoundException("Product not found");

            return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Stock);
        }
    }
}
