using MediatR;

namespace Application.CQRS.Notifications.Commands.MarkAllRead;

public record MarkAllReadCommand(int UserId) : IRequest;
