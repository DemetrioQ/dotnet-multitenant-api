namespace SaasApi.Application.Common.Interfaces;

/// <summary>
/// Builds the public storefront URL for a given tenant slug, using the
/// Storefront:PublicBaseUrl template (default https://{slug}.shop.demetrioq.com).
/// </summary>
public interface IStoreUrlBuilder
{
    string BuildUrl(string tenantSlug);
}
