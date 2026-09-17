using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Messages.Queries.GetUnreadMessageCount;

public class GetUnreadMessageCountQueryHandler(IMessageRepository messageRepository) : IRequestHandler<GetUnreadMessageCountQuery, int>
{
    public Task<int> Handle(GetUnreadMessageCountQuery request, CancellationToken cancellationToken) =>
        messageRepository.CountUnreadForUserAsync(request.UserId, cancellationToken);
}
