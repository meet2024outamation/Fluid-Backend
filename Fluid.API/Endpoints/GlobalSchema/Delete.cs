using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.GlobalSchema;

[Route("api/global-schemas")]
[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class Delete : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<bool>
{
    private readonly IGlobalSchemaService _globalSchemaService;

    public Delete(IGlobalSchemaService globalSchemaService)
    {
        _globalSchemaService = globalSchemaService;
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Delete a global schema",
        Description = "Soft deletes a global schema (sets IsActive to false). Requires Product Owner role.",
        OperationId = "GlobalSchema.Delete",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Global schema deleted successfully")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Global schema not found")]
    public async override Task<ActionResult<bool>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _globalSchemaService.DeleteAsync(id);
        return result.ToActionResult();
    }
}