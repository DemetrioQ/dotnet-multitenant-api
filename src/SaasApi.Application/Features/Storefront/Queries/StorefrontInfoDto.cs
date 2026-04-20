namespace SaasApi.Application.Features.Storefront.Queries
{
    public record StorefrontInfoDto(
        string Name,
        string Slug,
        string? SupportEmail,
        string? WebsiteUrl);
}
