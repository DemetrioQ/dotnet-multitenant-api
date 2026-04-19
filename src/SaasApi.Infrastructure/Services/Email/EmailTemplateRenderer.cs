using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;
using Scriban;
using Scriban.Runtime;

namespace SaasApi.Infrastructure.Services.Email;

public class EmailTemplateRenderer(IRepository<TenantEmailTemplate> templateRepo) : IEmailTemplateRenderer
{
    public async Task<RenderedEmail> RenderAsync(
        Guid tenantId,
        EmailTemplateType type,
        object model,
        CancellationToken ct = default)
    {
        var overrides = await templateRepo.FindGlobalAsync(
            t => t.TenantId == tenantId && t.Type == type, ct);
        var custom = overrides.FirstOrDefault();

        string subject;
        string body;
        bool enabled;

        if (custom is not null)
        {
            subject = custom.Subject;
            body = custom.BodyHtml;
            enabled = custom.Enabled;
        }
        else
        {
            var def = DefaultEmailTemplates.For(type);
            subject = def.Subject;
            body = def.BodyHtml;
            enabled = def.Enabled;
        }

        return await RenderSourceAsync(subject, body, enabled, model, ct);
    }

    public Task<RenderedEmail> RenderSourceAsync(
        string subject,
        string bodyHtml,
        bool enabled,
        object model,
        CancellationToken ct = default)
    {
        var renderedSubject = Render(subject, model);
        var renderedBody = Render(bodyHtml, model);
        return Task.FromResult(new RenderedEmail(renderedSubject, renderedBody, enabled));
    }

    public EmailTemplateDefault GetDefault(EmailTemplateType type)
    {
        var def = DefaultEmailTemplates.For(type);
        return new EmailTemplateDefault(def.Subject, def.BodyHtml, def.Enabled);
    }

    private static string Render(string template, object model)
    {
        var parsed = Template.Parse(template);
        if (parsed.HasErrors)
        {
            // Fall back to the raw template rather than failing the email delivery —
            // merchant template authoring mistakes shouldn't block a payment email.
            return template;
        }

        var scriptObject = new ScriptObject();
        scriptObject.Import(model, renamer: member => LowerSnakeCase(member.Name));
        var context = new TemplateContext { StrictVariables = false };
        context.PushGlobal(scriptObject);
        return parsed.Render(context);
    }

    // "CustomerFirstName" → "customer_first_name" (what Scriban expects by default).
    private static string LowerSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
