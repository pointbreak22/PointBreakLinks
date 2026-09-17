using Application.CQRS.Admin.DTOs;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetDashboard;

public record GetDashboardQuery : IRequest<AdminDashboardDto>;
