namespace SaasApi.Application.Common.Interfaces;

/// <summary>
/// Resolves the authenticated customer id from the current JWT.
/// IsCustomer is false for merchant/admin JWTs (or anonymous).
/// </summary>
public interface ICurrentCustomerService
{
    Guid CustomerId { get; }
    bool IsCustomer { get; }
}
