using Domain.Entities;

namespace Application.CQRS.Projects.DTOs;

// Same shape FOXLinks' ProjectResource uses for both the list and single-project response.
public record ProjectDto(
    int Id,
    string Name,
    string Type,
    string? Url,
    bool FlLossInsurance,
    bool FlLossAndIndexationInsurance,
    string TaskForVm,
    int TotalNumberOfLinks,
    int NumberOfLinksPosted,
    int NumberOfFrozenPosted,
    long SpentMoney)
{
    public static ProjectDto FromEntity(Project project) => new(
        project.Id,
        project.Name,
        project.Type,
        project.Url,
        project.HasLossInsurance,
        project.HasLossAndIndexationInsurance,
        project.TaskForVm,
        project.TotalLinks,
        project.LinksPosted,
        project.FrozenPosted,
        project.SpentMoney);
}
