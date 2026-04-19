using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class CustomerAddress : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? Label { get; private set; }

    public string Line1 { get; private set; } = default!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = default!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;

    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling { get; private set; }

    private CustomerAddress() { }

    public static CustomerAddress Create(
        Guid tenantId,
        Guid customerId,
        string? label,
        Address address,
        bool isDefaultShipping,
        bool isDefaultBilling) =>
        new()
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Label = label,
            Line1 = address.Line1,
            Line2 = address.Line2,
            City = address.City,
            Region = address.Region,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefaultShipping = isDefaultShipping,
            IsDefaultBilling = isDefaultBilling
        };

    public void Update(string? label, Address address)
    {
        Label = label;
        Line1 = address.Line1;
        Line2 = address.Line2;
        City = address.City;
        Region = address.Region;
        PostalCode = address.PostalCode;
        Country = address.Country;
    }

    public void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    public void SetDefaultBilling(bool value) => IsDefaultBilling = value;

    public Address ToAddress() => new(Line1, Line2, City, Region, PostalCode, Country);
}
