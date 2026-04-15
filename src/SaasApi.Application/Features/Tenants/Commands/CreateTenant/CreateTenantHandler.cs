using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Tenants.Commands.CreateTenant
{
    public class CreateTenantHandler(IRepository<Tenant> tenantRepo) : IRequestHandler<CreateTenantCommand, CreateTenantResult>
    {
        public async Task<CreateTenantResult> Handle(CreateTenantCommand request, CancellationToken ct)
        {
            var existing = await tenantRepo.FindAsync(u => u.Slug == request.Slug, ct);
            if (existing.Any())
                throw new ConflictException("A tenant with this slug already exists.");


            var tenant = Tenant.Create(request.Name, request.Slug);
            await tenantRepo.AddAsync(tenant, ct);
            await tenantRepo.SaveChangesAsync(ct);

            return new CreateTenantResult(tenant.Id);
        }
    }
}
