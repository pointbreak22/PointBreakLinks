using Application.Common;
using Application.CQRS.Projects.Commands.CreateProject;
using Application.CQRS.Projects.Commands.DeleteProject;
using Application.CQRS.Projects.Commands.UpdateProject;
using Application.CQRS.Projects.DTOs;
using Application.CQRS.Projects.Queries.GetMyProjects;
using Application.CQRS.Projects.Queries.GetProjectById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// Ported from FOXLinks' ProjectsController.
[Authorize]
[Route("api/projects")]
public class ProjectsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetMyProjects([FromQuery] int page = 1, [FromQuery] int perPage = 15)
    {
        var userId = GetCurrentUserId()!.Value;
        return Ok(await mediator.Send(new GetMyProjectsQuery(userId, page, perPage)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        var project = await mediator.Send(new GetProjectByIdQuery(id, userId));
        return Ok(new { data = project });
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var project = await mediator.Send(new CreateProjectCommand(
            userId, request.Name, request.Url, request.TaskForVm, request.FlLossInsurance, request.FlLossAndIndexationInsurance));
        return StatusCode(201, project);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, [FromBody] ProjectRequest request)
    {
        var userId = GetCurrentUserId()!.Value;
        var project = await mediator.Send(new UpdateProjectCommand(
            id, userId, request.Name, request.Url, request.TaskForVm, request.FlLossInsurance, request.FlLossAndIndexationInsurance));
        return Ok(project);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId()!.Value;
        await mediator.Send(new DeleteProjectCommand(id, userId));
        return Ok(new { message = "Project deleted" });
    }
}

// FOXLinks accepts both `task_for_vm` and `task_for_VM` (StoreProjectDTO::fromArray falls back
// between the two); we only need one shape since this is a fresh client, not compatibility with
// old callers — `task_for_vm` matches what create-edit-project-modal.vue actually sends.
public record ProjectRequest(string Name, string? Url, string TaskForVm, bool FlLossInsurance, bool FlLossAndIndexationInsurance);
