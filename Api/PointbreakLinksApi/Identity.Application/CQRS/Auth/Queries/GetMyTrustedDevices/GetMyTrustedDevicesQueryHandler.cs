using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetMyTrustedDevices;

public class GetMyTrustedDevicesQueryHandler(ITrustedDeviceRepository trustedDeviceRepository)
    : IRequestHandler<GetMyTrustedDevicesQuery, IReadOnlyList<TrustedDeviceDto>>
{
    public async Task<IReadOnlyList<TrustedDeviceDto>> Handle(GetMyTrustedDevicesQuery request, CancellationToken cancellationToken)
    {
        var devices = await trustedDeviceRepository.GetByUserAsync(request.UserId, cancellationToken);
        return devices.Select(TrustedDeviceDto.FromEntity).ToList();
    }
}
