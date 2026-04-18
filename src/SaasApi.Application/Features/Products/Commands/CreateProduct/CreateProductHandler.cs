using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SaasApi.Application.Common;
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

            var slug = await ResolveUniqueSlugAsync(request.Slug, request.Name, productRepo, ct);

            if (!string.IsNullOrWhiteSpace(request.Sku))
            {
                var skuClash = await productRepo.FindAsync(p => p.Sku == request.Sku, ct);
                if (skuClash.Any())
                    throw new ConflictException("Product with this SKU already exists in this tenant.");
            }

            var product = Product.Create(
                currentTenantService.TenantId,
                request.Name,
                slug,
                request.Description,
                request.Price,
                request.Stock,
                request.ImageUrl,
                request.Sku);
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

        private static async Task<string> ResolveUniqueSlugAsync(
            string? requested,
            string fallbackName,
            IRepository<Product> productRepo,
            CancellationToken ct)
        {
            var baseSlug = !string.IsNullOrWhiteSpace(requested)
                ? requested!
                : SlugGenerator.Slugify(fallbackName);

            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = "product";

            var candidate = baseSlug;
            var suffix = 2;
            while ((await productRepo.FindAsync(p => p.Slug == candidate, ct)).Any())
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;
            }
            return candidate;
        }
    }
}
