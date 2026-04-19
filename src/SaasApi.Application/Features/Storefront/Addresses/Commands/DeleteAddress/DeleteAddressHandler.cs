using MediatR;
using SaasApi.Application.Common.Exceptions;
using SaasApi.Application.Common.Interfaces;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Storefront.Addresses.Commands.DeleteAddress;

public class DeleteAddressHandler(
    IRepository<CustomerAddress> addressRepo,
    ICurrentCustomerService currentCustomer)
    : IRequestHandler<DeleteAddressCommand>
{
    public async Task Handle(DeleteAddressCommand request, CancellationToken ct)
    {
        var matches = await addressRepo.FindAsync(
            a => a.Id == request.Id && a.CustomerId == currentCustomer.CustomerId, ct);
        var address = matches.FirstOrDefault()
                      ?? throw new NotFoundException("Address not found.");

        addressRepo.Remove(address);
        await addressRepo.SaveChangesAsync(ct);

        // Deleting a default address: leave other addresses untouched — customer can set a new
        // default explicitly. Historical orders keep their snapshot, so no data loss.
    }
}
