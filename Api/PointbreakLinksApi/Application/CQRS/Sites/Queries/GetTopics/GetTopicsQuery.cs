using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Queries.GetTopics;

public record GetTopicsQuery : IRequest<IReadOnlyList<TopicDto>>;
