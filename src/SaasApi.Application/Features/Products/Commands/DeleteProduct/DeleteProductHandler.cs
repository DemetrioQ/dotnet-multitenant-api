using MediatR;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductHandler(
        IRepository<Product> productRepo,
        ICurrentTenantService currentTenantService
        ) : IRequestHandler<DeleteProductCommand>
    {
        async Task IRequestHandler<DeleteProductCommand>.Handle(DeleteProductCommand request, CancellationToken ct)
        {
            var existing = await productRepo.FindAsync(p =>  p.Id == request.Id, ct);
            if (!existing.Any())
                throw new InvalidOperationException("Product with this id does not exists in this tenant.");

            var product = existing.First();

            productRepo.Remove(product);

            await productRepo.SaveChangesAsync(ct);
        }

        
    }
}
