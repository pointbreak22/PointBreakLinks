using Application.CQRS.Stats.DTOs;
using MediatR;

namespace Application.CQRS.Stats.Queries.GetDynamicStats;

public record GetDynamicStatsQuery(string PageKey) : IRequest<IReadOnlyList<DynamicStatDto>>;
