using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetMyTrustedDevices;

public record GetMyTrustedDevicesQuery(int UserId) : IRequest<IReadOnlyList<TrustedDeviceDto>>;
