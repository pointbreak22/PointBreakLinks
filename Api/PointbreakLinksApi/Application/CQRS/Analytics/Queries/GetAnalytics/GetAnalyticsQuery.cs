using Application.CQRS.Analytics.DTOs;
using MediatR;

namespace Application.CQRS.Analytics.Queries.GetAnalytics;

public record GetAnalyticsQuery(int UserId) : IRequest<AnalyticsDto>;
