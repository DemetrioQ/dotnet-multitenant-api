using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.MerchantCustomers;

public record MerchantCustomerSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    bool IsEmailVerified,
    int OrderCount,
    decimal LifetimeSpend,
    DateTime CreatedAt);

public record MerchantCustomerOrderDto(
    Guid Id,
    string Number,
    string Status,
    decimal Total,
    DateTime CreatedAt);

public record MerchantCustomerDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    bool IsEmailVerified,
    int OrderCount,
    decimal LifetimeSpend,
    DateTime CreatedAt,
    IReadOnlyList<MerchantCustomerOrderDto> Orders);
