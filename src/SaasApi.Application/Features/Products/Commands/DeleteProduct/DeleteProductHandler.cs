using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductHandler(
        IRepository<Product> productRepo,
        IAuditService auditService
        ) : IRequestHandler<DeleteProductCommand>
    {
        async Task IRequestHandler<DeleteProductCommand>.Handle(DeleteProductCommand request, CancellationToken ct)
        {
            var existing = await productRepo.FindAsync(p => p.Id == request.Id, ct);
            if (!existing.Any())
                throw new NotFoundException("Product with this id does not exists in this tenant.");

            var product = existing.First();

            product.Deactivate();
            productRepo.Update(product);
            await productRepo.SaveChangesAsync(ct);

            await auditService.LogAsync("product.deactivated", "Product", request.Id, $"Deactivated {product.Name}", ct);
        }
    }
}
