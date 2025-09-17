using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Project;

public class UpdateStatusRequest
{
    [FromRoute]
    public int Id { get; set; }

    [FromBody]
    public UpdateProjectStatusRequest Request { get; set; } = new UpdateProjectStatusRequest();
}

[Route("api/projects")]
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateStatusRequest>
    .WithActionResult<ProjectResponse>
{
    private readonly IProjectService _projectService;

    public UpdateStatus(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPatch("{id:int}")]
    [SwaggerOperation(
        Summary = "Update project status",
        Description = "Updates the active status of a specific project by their ID",
        OperationId = "Project.UpdateStatus",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<ProjectResponse>> HandleAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.UpdateStatusAsync(request.Id, request.Request);
        return result.ToActionResult();
    }
}