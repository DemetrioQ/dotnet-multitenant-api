using MediatR;

namespace SaasApi.Application.Features.MerchantCustomers.Queries.GetMerchantCustomerById;

public record GetMerchantCustomerByIdQuery(Guid Id) : IRequest<MerchantCustomerDto>;
