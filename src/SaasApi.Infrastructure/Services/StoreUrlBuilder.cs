using Microsoft.Extensions.Configuration;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services;

public class StoreUrlBuilder(IConfiguration config) : IStoreUrlBuilder
{
    private const string DefaultTemplate = "https://{slug}.shop.demetrioq.com";

    public string BuildUrl(string tenantSlug)
    {
        var template = config["Storefront:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(template))
            template = DefaultTemplate;
        return template.Replace("{slug}", tenantSlug, StringComparison.OrdinalIgnoreCase);
    }
}
