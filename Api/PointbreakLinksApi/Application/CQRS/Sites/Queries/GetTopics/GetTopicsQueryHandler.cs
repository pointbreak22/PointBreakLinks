using Application.CQRS.Sites.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetTopics;

public class GetTopicsQueryHandler(ITopicRepository topicRepository) : IRequestHandler<GetTopicsQuery, IReadOnlyList<TopicDto>>
{
    public async Task<IReadOnlyList<TopicDto>> Handle(GetTopicsQuery request, CancellationToken cancellationToken)
    {
        var topics = await topicRepository.GetAllAsync(cancellationToken);
        return topics.Select(t => new TopicDto(t.Id, t.Name)).ToList();
    }
}
