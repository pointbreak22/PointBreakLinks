using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.RevokeTrustedDevice;

public record RevokeTrustedDeviceCommand(int UserId, int DeviceId) : IRequest;
