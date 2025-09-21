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
    .WithActionResult<TenantProjectsResponse>
{
    private readonly IProjectService _projectService;

    public List(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all projects grouped by tenants",
        Description = "Retrieves all projects organized by their respective tenants with tenant information",
        OperationId = "Project.List",
        Tags = new[] { "Projects" })
    ]
    [SwaggerResponse(200, "Projects retrieved successfully grouped by tenants", typeof(TenantProjectsResponse))]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<TenantProjectsResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.GetAllByTenantsAsync();
        return result.ToActionResult();
    }
}