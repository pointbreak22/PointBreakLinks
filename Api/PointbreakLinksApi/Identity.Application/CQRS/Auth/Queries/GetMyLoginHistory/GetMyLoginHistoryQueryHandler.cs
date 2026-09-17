using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetMyLoginHistory;

public class GetMyLoginHistoryQueryHandler(ILoginHistoryRepository loginHistoryRepository)
    : IRequestHandler<GetMyLoginHistoryQuery, IReadOnlyList<LoginHistoryEntryDto>>
{
    public async Task<IReadOnlyList<LoginHistoryEntryDto>> Handle(GetMyLoginHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await loginHistoryRepository.GetByUserAsync(request.UserId, request.Limit, cancellationToken);
        return entries.Select(LoginHistoryEntryDto.FromEntity).ToList();
    }
}
