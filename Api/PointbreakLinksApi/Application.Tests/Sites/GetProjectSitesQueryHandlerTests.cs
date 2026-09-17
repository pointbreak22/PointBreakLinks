using Application.CQRS.Sites.Queries.GetProjectSites;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repositories;
using Moq;

namespace Application.Tests.Sites;

// Regression coverage for a real gap this app closed relative to FOXLinks (see PROJECT_MAP.md):
// FOXLinks' getByProjectQuery() filtered purchased sites only by project_id, with no check that
// the project actually belongs to the caller — any authenticated user could read another
// buyer's order list just by guessing a project id.
public class GetProjectSitesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ProjectNotOwnedByCaller_ThrowsNotFound()
    {
        var projectRepo = new Mock<IProjectRepository>();
        projectRepo.Setup(r => r.GetByIdForOwnerAsync(5, 999, It.IsAny<CancellationToken>())).ReturnsAsync((Project?)null);
        var orderRepo = new Mock<IPurchasedSiteRepository>();

        var handler = new GetProjectSitesQueryHandler(projectRepo.Object, orderRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProjectSitesQuery(ProjectId: 5, UserId: 999, Page: 1, PerPage: 10), CancellationToken.None));

        orderRepo.Verify(r => r.GetByProjectAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnedProject_ReturnsPagedOrders()
    {
        var project = new Project { Id = 5, UserId = 20 };
        var projectRepo = new Mock<IProjectRepository>();
        projectRepo.Setup(r => r.GetByIdForOwnerAsync(5, 20, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var order = TestBuilders.Order(id: 1, buyerId: 20);
        var orderRepo = new Mock<IPurchasedSiteRepository>();
        orderRepo.Setup(r => r.GetByProjectAsync(5, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<PurchasedSite> { order }, 1));

        var handler = new GetProjectSitesQueryHandler(projectRepo.Object, orderRepo.Object);
        var result = await handler.Handle(new GetProjectSitesQuery(ProjectId: 5, UserId: 20, Page: 1, PerPage: 10), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Total);
    }
}
