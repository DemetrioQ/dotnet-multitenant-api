using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductHandler(
        IRepository<Product> productRepo,
        IRepository<TenantOnboardingStatus> onboardingRepo,
        ICurrentTenantService currentTenantService,
        IAuditService auditService,
        IMemoryCache cache
        )
        : IRequestHandler<CreateProductCommand, CreateProductResult>
    {
        public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken ct)
        {
            var existing = await productRepo.FindAsync(p => p.Name == request.Name, ct);
            if (existing.Any())
                throw new ConflictException("Product with this name already exists in this tenant.");

            var product = Product.Create(currentTenantService.TenantId, request.Name, request.Description, request.Price, request.Stock);
            await productRepo.AddAsync(product);

            var statuses = await onboardingRepo.FindAsync(_ => true, ct);
            var status = statuses.FirstOrDefault();
            if (status is not null && !status.FirstProductCreated)
            {
                status.CompleteFirstProduct();
                onboardingRepo.Update(status);
            }

            await productRepo.SaveChangesAsync(ct);

            cache.Remove($"onboarding:{currentTenantService.TenantId}");

            await auditService.LogAsync("product.created", "Product", product.Id, $"Created {request.Name}", ct);

            return new CreateProductResult(product.Id);
        }
    }
}
