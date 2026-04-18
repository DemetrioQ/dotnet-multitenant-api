namespace SaasApi.Domain.Entities;

public class Address
{
    public string Line1 { get; private set; } = default!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = default!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;

    private Address() { }

    public Address(string line1, string? line2, string city, string? region, string postalCode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
    }
}
