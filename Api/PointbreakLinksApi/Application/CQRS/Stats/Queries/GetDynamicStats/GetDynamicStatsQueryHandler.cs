using Application.CQRS.Stats.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Stats.Queries.GetDynamicStats;

public class GetDynamicStatsQueryHandler(IDynamicStatRepository dynamicStatRepository)
    : IRequestHandler<GetDynamicStatsQuery, IReadOnlyList<DynamicStatDto>>
{
    public async Task<IReadOnlyList<DynamicStatDto>> Handle(GetDynamicStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await dynamicStatRepository.GetByPageKeyAsync(request.PageKey, cancellationToken);
        return stats.Select(DynamicStatDto.FromEntity).ToList();
    }
}
