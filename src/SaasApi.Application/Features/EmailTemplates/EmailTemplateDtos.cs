using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.EmailTemplates;

/// <summary>
/// Metadata describing a single template type and (if present) the merchant's current override.
/// Returned as a list so the dashboard can render a table of all customizable email types.
/// </summary>
public record EmailTemplateListItemDto(
    EmailTemplateType Type,
    string DefaultSubject,
    string DefaultBodyHtml,
    bool DefaultEnabled,
    string? CustomSubject,
    string? CustomBodyHtml,
    bool? CustomEnabled,
    DateTime? CustomUpdatedAt,
    IReadOnlyList<string> Placeholders);

public record EmailTemplateDetailDto(
    EmailTemplateType Type,
    string Subject,
    string BodyHtml,
    bool Enabled,
    bool IsCustom,
    IReadOnlyList<string> Placeholders);

public record EmailTemplatePreviewDto(
    string Subject,
    string BodyHtml,
    bool Enabled);
