using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class TenantEmailTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public EmailTemplateType Type { get; private set; }
    public string Subject { get; private set; } = default!;
    public string BodyHtml { get; private set; } = default!;
    public bool Enabled { get; private set; } = true;

    private TenantEmailTemplate() { }

    public static TenantEmailTemplate Create(Guid tenantId, EmailTemplateType type, string subject, string bodyHtml, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        if (string.IsNullOrWhiteSpace(bodyHtml))
            throw new ArgumentException("Body HTML cannot be empty.", nameof(bodyHtml));

        return new TenantEmailTemplate
        {
            TenantId = tenantId,
            Type = type,
            Subject = subject,
            BodyHtml = bodyHtml,
            Enabled = enabled
        };
    }

    public void Update(string subject, string bodyHtml, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        if (string.IsNullOrWhiteSpace(bodyHtml))
            throw new ArgumentException("Body HTML cannot be empty.", nameof(bodyHtml));

        Subject = subject;
        BodyHtml = bodyHtml;
        Enabled = enabled;
    }
}
