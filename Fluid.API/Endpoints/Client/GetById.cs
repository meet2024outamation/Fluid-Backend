using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;

namespace Fluid.API.Endpoints.Project;

[Route("api/projects")]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<ProjectResponse>
{
    private readonly IProjectService _projectService;

    public GetById(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get project by ID",
        Description = "Retrieves a specific project by their ID",
        OperationId = "Project.GetById",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<ProjectResponse>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}