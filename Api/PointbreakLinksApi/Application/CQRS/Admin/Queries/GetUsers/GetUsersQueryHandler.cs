using Application.Common;
using Application.CQRS.Admin.DTOs;
using Domain.Repositories;
using MediatR;

namespace Application.CQRS.Admin.Queries.GetUsers;

public class GetUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUsersQuery, PagedResult<AdminUserDto>>
{
    public async Task<PagedResult<AdminUserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await userRepository.GetAllPaginatedAsync(request.Page, request.PerPage, request.Search, request.Role, cancellationToken);
        return PagedResult<AdminUserDto>.Create(items.Select(AdminUserDto.FromEntity).ToList(), total, request.Page, request.PerPage);
    }
}
