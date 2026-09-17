using Application.CQRS.Admin.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetDashboard;

public class GetDashboardQueryHandler(IAdminDashboardRepository dashboardRepository) : IRequestHandler<GetDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var stats = await dashboardRepository.GetStatsAsync(cancellationToken);

        return new AdminDashboardDto(
            stats.TotalUsers,
            stats.TotalProjects,
            stats.TotalActiveSites,
            stats.TotalOrders,
            stats.TotalRevenue,
            stats.PendingModerationCount,
            stats.UsersByRole.Select(r => new AdminRoleCountDto(r.Name, r.DisplayName, r.Count)).ToList(),
            stats.RecentUsers.Select(u => new AdminRecentUserDto(u.Id, u.Name, u.Email, u.CreatedAt)).ToList(),
            stats.RecentOrders.Select(o => new AdminRecentOrderDto(o.Id, o.SiteUrl, o.BuyerName, o.FinalPrice, o.StatusDescription, o.CreatedAt)).ToList());
    }
}
