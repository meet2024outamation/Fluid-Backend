using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;

namespace Fluid.API.Endpoints.Project;

public class UpdateByIdRequest
{
    [FromRoute]
    public int Id { get; set; }
    
    [FromBody]
    public UpdateProjectRequest Request { get; set; } = null!;
}

[Route("api/projects")]
public class UpdateById : EndpointBaseAsync
    .WithRequest<UpdateByIdRequest>
    .WithActionResult<ProjectResponse>
{
    private readonly IProjectService _projectService;

    public UpdateById(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Update project by ID",
        Description = "Updates a specific project by their ID",
        OperationId = "Project.UpdateById",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<ProjectResponse>> HandleAsync(
        UpdateByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.UpdateAsync(request.Id, request.Request);
        return result.ToActionResult();
    }
}