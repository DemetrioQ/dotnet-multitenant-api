using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common.Interfaces;

public record RenderedEmail(string Subject, string HtmlBody, bool Enabled);

public record EmailTemplateDefault(string Subject, string BodyHtml, bool Enabled);

/// <summary>
/// Resolves a tenant-customizable email template and renders it with the given model.
/// If the tenant has an override row for the type, that is used; otherwise the default
/// template baked into the codebase is used. Either source is run through Scriban, so
/// placeholders like <c>{{ customer.first_name }}</c> work in both.
/// </summary>
public interface IEmailTemplateRenderer
{
    Task<RenderedEmail> RenderAsync(
        Guid tenantId,
        EmailTemplateType type,
        object model,
        CancellationToken ct = default);

    Task<RenderedEmail> RenderSourceAsync(
        string subject,
        string bodyHtml,
        bool enabled,
        object model,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the built-in (default) template copy for a type. Used by the merchant
    /// UI to display the current default alongside any override.
    /// </summary>
    EmailTemplateDefault GetDefault(EmailTemplateType type);
}
