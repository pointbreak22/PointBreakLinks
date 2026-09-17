using Application.CQRS.Analytics.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Analytics.Queries.GetAnalytics;

public class GetAnalyticsQueryHandler(IAnalyticsRepository analyticsRepository) : IRequestHandler<GetAnalyticsQuery, AnalyticsDto>
{
    private static readonly string[] MonthAbbreviations =
        ["янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек"];

    public async Task<AnalyticsDto> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var stats = await analyticsRepository.GetStatsAsync(request.UserId, cancellationToken);

        return new AnalyticsDto(
            stats.TotalSpent,
            stats.TotalEarned,
            stats.ActiveProjects,
            stats.ActiveSites,
            stats.SpendByMonth.Select(FormatMonth).ToList(),
            stats.EarnedByMonth.Select(FormatMonth).ToList(),
            stats.OrdersByStatus.Select(s => new StatusBreakdownDto(s.StatusName, s.StatusDescription ?? s.StatusName, s.Count)).ToList(),
            stats.Projects.Select(p => new ProjectSummaryDto(p.Id, p.Name, p.Spent, p.OrdersCount, p.LinksPosted)).ToList());
    }

    private static MonthlyPointDto FormatMonth(AnalyticsMonthlyPoint point) =>
        new($"{MonthAbbreviations[point.Month - 1]} {point.Year}", point.Value);
}
