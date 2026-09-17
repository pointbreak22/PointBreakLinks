using MediatR;

namespace Application.CQRS.Projects.Commands.DeleteProject;

// A hard delete, matching FOXLinks (Project::delete()) — unlike Sites' DeactivateSite, which
// deliberately deviates from a hard delete because deleting a Site would cascade-wipe order
// history. Deleting a Project cascading to its own PurchasedSites is the source's actual
// behavior (purchased_sites.project_id is cascadeOnDelete in the Laravel migration too).
public record DeleteProjectCommand(int ProjectId, int UserId) : IRequest;
