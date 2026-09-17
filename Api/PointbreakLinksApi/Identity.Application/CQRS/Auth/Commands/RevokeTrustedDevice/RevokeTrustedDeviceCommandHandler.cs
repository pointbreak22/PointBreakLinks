using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.RevokeTrustedDevice;

public class RevokeTrustedDeviceCommandHandler(ITrustedDeviceRepository trustedDeviceRepository)
    : IRequestHandler<RevokeTrustedDeviceCommand>
{
    public async Task Handle(RevokeTrustedDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await trustedDeviceRepository.GetByIdAsync(request.DeviceId, request.UserId, cancellationToken)
                     ?? throw new NotFoundException("TrustedDevice", request.DeviceId);

        await trustedDeviceRepository.DeleteAsync(device, cancellationToken);
        await trustedDeviceRepository.SaveChangesAsync(cancellationToken);
    }
}
