namespace SaasApi.Application.Features.Storefront.Queries
{
    public record StorefrontInfoDto(
        string Name,
        string Slug,
        string Currency,
        string Timezone,
        string? SupportEmail,
        string? WebsiteUrl);
}
