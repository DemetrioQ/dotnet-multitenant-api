using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services;

public class CurrentCustomerService(IHttpContextAccessor httpContextAccessor) : ICurrentCustomerService
{
    public Guid CustomerId
    {
        get
        {
            if (!IsCustomer) return Guid.Empty;
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return sub is not null && Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public bool IsCustomer =>
        httpContextAccessor.HttpContext?.User.FindFirst("sub_type")?.Value == "customer";
}
