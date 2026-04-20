namespace SaasApi.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? SupportEmail { get; set; }
        public string? WebsiteUrl { get; set; }
    }
}
