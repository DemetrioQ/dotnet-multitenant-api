namespace SaasApi.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Fulfilled = 2,
    Canceled = 3,
    Refunded = 4
}
