namespace SaasApi.Application.Common.Interfaces;

/// <summary>
/// Model fed to CustomerVerification templates. Field names should match exactly what
/// merchants will use in their Scriban placeholders (e.g. <c>{{ store_name }}</c>).
/// Scriban lower-snake-cases .NET PascalCase member names by default.
/// </summary>
public record CustomerVerificationModel(
    string StoreName,
    string StoreUrl,
    string CustomerFirstName,
    string CustomerEmail,
    string VerificationLink);

public record CustomerPasswordResetModel(
    string StoreName,
    string StoreUrl,
    string CustomerFirstName,
    string CustomerEmail,
    string ResetLink);

public record OrderEmailModel(
    string StoreName,
    string StoreUrl,
    string CustomerFirstName,
    string CustomerEmail,
    string OrderNumber,
    decimal OrderTotal,
    string Currency,
    string OrderDetailsUrl);
