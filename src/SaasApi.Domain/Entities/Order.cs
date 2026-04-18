using SaasApi.Domain.Common;

namespace SaasApi.Domain.Entities;

public class Order : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Number { get; private set; } = default!;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal Subtotal { get; private set; }
    public decimal Total { get; private set; }

    public string ShippingLine1 { get; private set; } = default!;
    public string? ShippingLine2 { get; private set; }
    public string ShippingCity { get; private set; } = default!;
    public string? ShippingRegion { get; private set; }
    public string ShippingPostalCode { get; private set; } = default!;
    public string ShippingCountry { get; private set; } = default!;

    public string BillingLine1 { get; private set; } = default!;
    public string? BillingLine2 { get; private set; }
    public string BillingCity { get; private set; } = default!;
    public string? BillingRegion { get; private set; }
    public string BillingPostalCode { get; private set; } = default!;
    public string BillingCountry { get; private set; } = default!;

    public DateTime? PaidAt { get; private set; }
    public DateTime? FulfilledAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    public string? PaymentProvider { get; private set; }
    public string? PaymentSessionId { get; private set; }

    private Order() { }

    public static Order Create(
        Guid tenantId,
        Guid customerId,
        string number,
        decimal subtotal,
        Address shipping,
        Address billing) =>
        new()
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Number = number,
            Subtotal = subtotal,
            Total = subtotal, // no tax/shipping yet
            ShippingLine1 = shipping.Line1,
            ShippingLine2 = shipping.Line2,
            ShippingCity = shipping.City,
            ShippingRegion = shipping.Region,
            ShippingPostalCode = shipping.PostalCode,
            ShippingCountry = shipping.Country,
            BillingLine1 = billing.Line1,
            BillingLine2 = billing.Line2,
            BillingCity = billing.City,
            BillingRegion = billing.Region,
            BillingPostalCode = billing.PostalCode,
            BillingCountry = billing.Country
        };

    public Address GetShippingAddress() =>
        new(ShippingLine1, ShippingLine2, ShippingCity, ShippingRegion, ShippingPostalCode, ShippingCountry);

    public Address GetBillingAddress() =>
        new(BillingLine1, BillingLine2, BillingCity, BillingRegion, BillingPostalCode, BillingCountry);

    public void AttachPaymentSession(string provider, string sessionId)
    {
        PaymentProvider = provider;
        PaymentSessionId = sessionId;
    }

    public void MarkPaid()
    {
        Status = OrderStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkFulfilled()
    {
        Status = OrderStatus.Fulfilled;
        FulfilledAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = OrderStatus.Canceled;
        CanceledAt = DateTime.UtcNow;
    }
}
