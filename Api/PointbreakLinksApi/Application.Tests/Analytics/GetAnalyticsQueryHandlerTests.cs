using Application.CQRS.Analytics.Queries.GetAnalytics;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Analytics;

// FormatMonth indexes a 12-element abbreviation array by `point.Month - 1` — an easy off-by-one
// to reintroduce (e.g. dropping the "- 1" or getting the array boundary wrong), so both ends of
// the year are covered explicitly rather than just a middle-of-the-year sanity check.
public class GetAnalyticsQueryHandlerTests
{
    [Theory]
    [InlineData(1, "янв")]
    [InlineData(6, "июн")]
    [InlineData(12, "дек")]
    public async Task Handle_FormatsEveryMonthWithoutOffByOne(int month, string expectedAbbreviation)
    {
        var stats = new AnalyticsStats(
            TotalSpent: 0m,
            TotalEarned: 0m,
            ActiveProjects: 0,
            ActiveSites: 0,
            SpendByMonth: [new AnalyticsMonthlyPoint(2026, month, 500m)],
            EarnedByMonth: [],
            OrdersByStatus: [],
            Projects: []);

        var repo = new Mock<IAnalyticsRepository>();
        repo.Setup(r => r.GetStatsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(stats);

        var handler = new GetAnalyticsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetAnalyticsQuery(1), CancellationToken.None);

        Assert.Equal($"{expectedAbbreviation} 2026", result.SpendByMonth.Single().Month);
        Assert.Equal(500m, result.SpendByMonth.Single().Value);
    }

    [Fact]
    public async Task Handle_StatusBreakdown_FallsBackToNameWhenDescriptionMissing()
    {
        var stats = new AnalyticsStats(0m, 0m, 0, 0, [], [],
            OrdersByStatus: [new AnalyticsStatusBreakdown("work", null, 3)],
            Projects: []);

        var repo = new Mock<IAnalyticsRepository>();
        repo.Setup(r => r.GetStatsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(stats);

        var handler = new GetAnalyticsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetAnalyticsQuery(1), CancellationToken.None);

        var breakdown = result.OrdersByStatus.Single();
        Assert.Equal("work", breakdown.Description); // null Description degrades to the raw status name
        Assert.Equal(3, breakdown.Count);
    }
}
