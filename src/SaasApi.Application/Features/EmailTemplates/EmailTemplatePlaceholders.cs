using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.EmailTemplates;

/// <summary>
/// Documentation / UI hints for the merchant: which placeholder names each template type
/// accepts. Matches the model record field names (snake-cased). Used by the list/detail
/// endpoints so the dashboard can show a legend alongside the editor.
/// </summary>
public static class EmailTemplatePlaceholders
{
    private static readonly IReadOnlyList<string> Customer =
        new[] { "store_name", "store_url", "customer_first_name", "customer_email" };

    public static IReadOnlyList<string> ForType(EmailTemplateType type) => type switch
    {
        EmailTemplateType.CustomerVerification =>
            new[] { "store_name", "store_url", "customer_first_name", "customer_email", "verification_link" },
        EmailTemplateType.CustomerPasswordReset =>
            new[] { "store_name", "store_url", "customer_first_name", "customer_email", "reset_link" },
        EmailTemplateType.OrderPlaced or
        EmailTemplateType.OrderPaid or
        EmailTemplateType.OrderFulfilled =>
            new[] { "store_name", "store_url", "customer_first_name", "customer_email",
                "order_number", "order_total", "currency", "order_details_url" },
        _ => Customer
    };

    /// <summary>
    /// A sample model for previewing a template. Values are plausible demo content so
    /// merchants can see what the rendered email will look like before saving.
    /// </summary>
    public static object SampleModel(EmailTemplateType type, string storeName, string storeUrl) => type switch
    {
        EmailTemplateType.CustomerVerification => new
        {
            StoreName = storeName,
            StoreUrl = storeUrl,
            CustomerFirstName = "Alex",
            CustomerEmail = "alex@example.com",
            VerificationLink = $"{storeUrl.TrimEnd('/')}/verify-email?token=sample"
        },
        EmailTemplateType.CustomerPasswordReset => new
        {
            StoreName = storeName,
            StoreUrl = storeUrl,
            CustomerFirstName = "Alex",
            CustomerEmail = "alex@example.com",
            ResetLink = $"{storeUrl.TrimEnd('/')}/reset-password?token=sample"
        },
        _ => new
        {
            StoreName = storeName,
            StoreUrl = storeUrl,
            CustomerFirstName = "Alex",
            CustomerEmail = "alex@example.com",
            OrderNumber = "ORD-DEMO12345",
            OrderTotal = 42.50m,
            Currency = "USD",
            OrderDetailsUrl = $"{storeUrl.TrimEnd('/')}/orders/sample"
        }
    };
}
