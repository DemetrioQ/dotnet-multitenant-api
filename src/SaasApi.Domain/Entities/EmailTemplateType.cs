namespace SaasApi.Domain.Entities;

/// <summary>
/// Types of customer-facing email that a tenant can customize.
/// Stored in the DB as the string name via a value converter.
/// </summary>
public enum EmailTemplateType
{
    CustomerVerification = 0,
    CustomerPasswordReset = 1,
    OrderPlaced = 2,
    OrderPaid = 3,
    OrderFulfilled = 4
}
