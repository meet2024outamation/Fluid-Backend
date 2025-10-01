using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Authorization;

namespace Fluid.API.Endpoints.Project;

[Route("api/projects")]
[RequireTenantAccess]
public class Delete : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<bool>
{
    private readonly IProjectService _projectService;

    public Delete(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Delete project by ID",
        Description = "Deletes a specific project by their ID. Cannot delete if project has associated batches or field mappings.",
        OperationId = "Project.Delete",
        Tags = new[] { "Projects" })
    ]
    public async override Task<ActionResult<bool>> HandleAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _projectService.DeleteAsync(id);
        return result.ToActionResult();
    }
}