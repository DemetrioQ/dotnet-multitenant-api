using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductHandler(
        IRepository<Product> productRepo,
        ICurrentTenantService currentTenantService
        ) : IRequestHandler<UpdateProductCommand, UpdateProductResult>
    {
        public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken ct)
        {
            var existing = await productRepo.FindAsync(p => p.Id == request.Id, ct);
            if (!existing.Any())
                throw new NotFoundException("Product with this id does not exists in this tenant.");

            var product = existing.First();

            product.Update(request.Name, request.Description, request.Price, request.Stock);
            productRepo.Update(product);
            await productRepo.SaveChangesAsync(ct);

            return new UpdateProductResult(product.Id);
        }
    }
}
