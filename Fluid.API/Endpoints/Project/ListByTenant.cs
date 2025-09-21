using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Client;

[Route("api/projects")]
public class ListByTenant : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<ProjectListResponse>>
{
    private readonly IProjectService _projectService;

    public ListByTenant(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("by-tenant")]
    [SwaggerOperation(
        Summary = "Get all projects (simple list)",
        Description = "Retrieves a simple list of all projects in the current tenant context",
        OperationId = "Project.ListSimple",
        Tags = new[] { "Projects" })
    ]
    [SwaggerResponse(200, "Projects retrieved successfully", typeof(List<ProjectListResponse>))]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<List<ProjectListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.GetAllAsync();
        return result.ToActionResult();
    }
}