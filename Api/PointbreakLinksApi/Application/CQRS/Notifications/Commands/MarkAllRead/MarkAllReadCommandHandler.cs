using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Notifications.Commands.MarkAllRead;

public class MarkAllReadCommandHandler(INotificationRepository notificationRepository) : IRequestHandler<MarkAllReadCommand>
{
    public Task Handle(MarkAllReadCommand request, CancellationToken cancellationToken) =>
        notificationRepository.MarkAllReadAsync(request.UserId, cancellationToken);
}
