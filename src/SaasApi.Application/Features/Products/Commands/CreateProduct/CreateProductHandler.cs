using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductHandler(
        IRepository<Product> productRepo,
        ICurrentTenantService currentTenantService
        )
        : IRequestHandler<CreateProductCommand, CreateProductResult>
    {
        public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken ct)
        {
            var existing = await productRepo.FindAsync(p => p.Name == request.Name, ct);
            if (existing.Any())
                throw new InvalidOperationException("Product with this name already exists in this tenant.");

            var product = Product.Create(currentTenantService.TenantId, request.Name, request.Description, request.Price, request.Stock);

            await productRepo.AddAsync(product);
            await productRepo.SaveChangesAsync(ct);

            return new CreateProductResult(product.Id);
        }
    }
}
