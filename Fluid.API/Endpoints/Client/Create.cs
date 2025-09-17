using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Project;

namespace Fluid.API.Endpoints.Project;

[Route("api/projects")]
public class Create : EndpointBaseAsync
    .WithRequest<CreateProjectRequest>
    .WithActionResult<ProjectResponse>
{
    private readonly IProjectService _projectService;
    private readonly ICurrentUserService _currentUserService;

    public Create(IProjectService projectService, ICurrentUserService currentUserService)
    {
        _projectService = projectService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new project",
        Description = "Creates a new project in the system",
        OperationId = "Project.Create",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<ProjectResponse>> HandleAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _projectService.CreateAsync(request, currentUserId);
        return result.ToActionResult();
    }
}