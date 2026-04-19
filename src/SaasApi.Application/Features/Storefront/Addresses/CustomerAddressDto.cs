using SaasApi.Domain.Entities;

namespace SaasApi.Application.Features.Storefront.Addresses;

public record CustomerAddressDto(
    Guid Id,
    string? Label,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country,
    bool IsDefaultShipping,
    bool IsDefaultBilling)
{
    public static CustomerAddressDto FromEntity(CustomerAddress a) =>
        new(a.Id, a.Label, a.Line1, a.Line2, a.City, a.Region, a.PostalCode, a.Country,
            a.IsDefaultShipping, a.IsDefaultBilling);
}
