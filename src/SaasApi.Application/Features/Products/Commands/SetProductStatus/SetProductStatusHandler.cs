using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.SetProductStatus;

public class SetProductStatusHandler(
    IRepository<Product> productRepo,
    IAuditService auditService)
    : IRequestHandler<SetProductStatusCommand>
{
    public async Task Handle(SetProductStatusCommand request, CancellationToken ct)
    {
        var products = await productRepo.FindAsync(p => p.Id == request.Id, ct);
        var product = products.FirstOrDefault();
        if (product is null)
            throw new NotFoundException("Product not found.");

        if (request.IsActive)
            product.Activate();
        else
            product.Deactivate();

        productRepo.Update(product);
        await productRepo.SaveChangesAsync(ct);

        var action = request.IsActive ? "product.activated" : "product.deactivated";
        await auditService.LogAsync(action, "Product", product.Id, $"{action}: {product.Name}", ct);
    }
}
