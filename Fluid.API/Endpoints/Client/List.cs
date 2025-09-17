using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;

namespace Fluid.API.Endpoints.Project;

[Route("api/projects")]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<ProjectListResponse>>
{
    private readonly IProjectService _projectService;

    public List(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all projects",
        Description = "Retrieves a list of all projects in the system",
        OperationId = "Project.List",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<List<ProjectListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.GetAllAsync();
        return result.ToActionResult();
    }
}