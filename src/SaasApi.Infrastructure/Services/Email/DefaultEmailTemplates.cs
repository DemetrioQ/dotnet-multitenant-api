using SaasApi.Domain.Entities;

namespace SaasApi.Infrastructure.Services.Email;

/// <summary>
/// Ships the built-in copy for every EmailTemplateType. Used when a tenant hasn't
/// overridden the template. Scriban is run over both defaults and overrides so both
/// support the same placeholder syntax (e.g. <c>{{ store_name }}</c>).
/// </summary>
public static class DefaultEmailTemplates
{
    public record Default(string Subject, string BodyHtml, bool Enabled);

    public static Default For(EmailTemplateType type) => type switch
    {
        EmailTemplateType.CustomerVerification => new(
            Subject: "Confirm your email for {{ store_name }}",
            BodyHtml:
                "<p>Hi {{ customer_first_name }},</p>" +
                "<p>Thanks for signing up at <strong>{{ store_name }}</strong>!</p>" +
                "<p>Confirm your email address to finish setting up your account:</p>" +
                "<p><a href=\"{{ verification_link }}\">Confirm my email</a></p>" +
                "<p>If you didn't create an account at {{ store_name }}, you can safely ignore this message.</p>",
            Enabled: true),

        EmailTemplateType.CustomerPasswordReset => new(
            Subject: "Reset your {{ store_name }} password",
            BodyHtml:
                "<p>Hi {{ customer_first_name }},</p>" +
                "<p>We received a request to reset your password for your account at <strong>{{ store_name }}</strong>.</p>" +
                "<p><a href=\"{{ reset_link }}\">Choose a new password</a></p>" +
                "<p>This link expires in 1 hour. If you didn't request a reset, you can ignore this message.</p>",
            Enabled: true),

        EmailTemplateType.OrderPlaced => new(
            Subject: "We got your order, {{ customer_first_name }} — #{{ order_number }}",
            BodyHtml:
                "<p>Hi {{ customer_first_name }},</p>" +
                "<p>Thanks for your order at <strong>{{ store_name }}</strong>! We've received it and will follow up once payment is confirmed.</p>" +
                "<p><strong>Order #{{ order_number }}</strong><br/>Total: {{ order_total }} {{ currency }}</p>" +
                "<p><a href=\"{{ order_details_url }}\">View your order</a></p>",
            // Disabled by default — Stripe flow fires OrderPaid which is usually the
            // single email a customer needs. Merchants can enable this for pre-payment
            // manual-fulfilment flows.
            Enabled: false),

        EmailTemplateType.OrderPaid => new(
            Subject: "Payment confirmed for your {{ store_name }} order #{{ order_number }}",
            BodyHtml:
                "<p>Hi {{ customer_first_name }},</p>" +
                "<p>We've received your payment — thanks for shopping at <strong>{{ store_name }}</strong>!</p>" +
                "<p><strong>Order #{{ order_number }}</strong><br/>Total: {{ order_total }} {{ currency }}</p>" +
                "<p>We'll let you know when it ships. You can also check the latest status here:</p>" +
                "<p><a href=\"{{ order_details_url }}\">View your order</a></p>",
            Enabled: true),

        EmailTemplateType.OrderFulfilled => new(
            Subject: "Your {{ store_name }} order #{{ order_number }} is on its way",
            BodyHtml:
                "<p>Hi {{ customer_first_name }},</p>" +
                "<p>Your order from <strong>{{ store_name }}</strong> has shipped.</p>" +
                "<p><strong>Order #{{ order_number }}</strong></p>" +
                "<p><a href=\"{{ order_details_url }}\">Track your order</a></p>",
            Enabled: true),

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
