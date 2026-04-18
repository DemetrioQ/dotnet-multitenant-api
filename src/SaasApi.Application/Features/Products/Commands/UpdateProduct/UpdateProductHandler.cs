using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductHandler(
        IRepository<Product> productRepo,
        IAuditService auditService
        ) : IRequestHandler<UpdateProductCommand, UpdateProductResult>
    {
        public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken ct)
        {
            var existing = await productRepo.FindAsync(p => p.Id == request.Id, ct);
            if (!existing.Any())
                throw new NotFoundException("Product with this id does not exists in this tenant.");

            var product = existing.First();

            product.Update(request.Name, request.Description, request.Price, request.Stock);

            if (!string.IsNullOrWhiteSpace(request.Slug) && request.Slug != product.Slug)
            {
                var slugClash = await productRepo.FindAsync(p => p.Slug == request.Slug && p.Id != request.Id, ct);
                if (slugClash.Any())
                    throw new ConflictException("Product with this slug already exists in this tenant.");
                product.UpdateSlug(request.Slug);
            }

            if (!string.IsNullOrWhiteSpace(request.Sku) && request.Sku != product.Sku)
            {
                var skuClash = await productRepo.FindAsync(p => p.Sku == request.Sku && p.Id != request.Id, ct);
                if (skuClash.Any())
                    throw new ConflictException("Product with this SKU already exists in this tenant.");
            }
            product.UpdateSku(request.Sku);

            product.UpdateImageUrl(request.ImageUrl);

            productRepo.Update(product);
            await productRepo.SaveChangesAsync(ct);

            await auditService.LogAsync("product.updated", "Product", product.Id, $"Updated {request.Name}", ct);

            return new UpdateProductResult(product.Id);
        }
    }
}
