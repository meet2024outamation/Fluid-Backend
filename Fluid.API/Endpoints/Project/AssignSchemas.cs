using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;
using Fluid.API.Authorization;

namespace Fluid.API.Endpoints.Project;

[Route("api/projects")]
[RequireTenantAccess]
public class AssignSchemas : EndpointBaseAsync
    .WithRequest<AssignSchemasRequest>
    .WithActionResult<ProjectSchemaAssignmentResponse>
{
    private readonly IProjectService _projectService;
    private readonly ICurrentUserService _currentUserService;

    public AssignSchemas(IProjectService projectService, ICurrentUserService currentUserService)
    {
        _projectService = projectService;
        _currentUserService = currentUserService;
    }

    [HttpPost("assign-schemas")]
    [SwaggerOperation(
        Summary = "Assign schemas to a project",
        Description = "Assigns a list of schemas to a specific project. This will replace any existing schema assignments for the project.",
        OperationId = "Project.AssignSchemas",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<ProjectSchemaAssignmentResponse>> HandleAsync(
        AssignSchemasRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _projectService.AssignSchemasAsync(request, currentUserId);
        return result.ToActionResult();
    }
}