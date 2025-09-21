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
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateStatusRequest>
    .WithActionResult<bool>
{
    private readonly IGlobalSchemaService _globalSchemaService;

    public UpdateStatus(IGlobalSchemaService globalSchemaService)
    {
        _globalSchemaService = globalSchemaService;
    }

    [HttpPatch("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Update global schema status",
        Description = "Activates or deactivates a global schema. Requires Product Owner role.",
        OperationId = "GlobalSchema.UpdateStatus",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Schema status updated successfully")]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Global schema not found")]
    public async override Task<ActionResult<bool>> HandleAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _globalSchemaService.UpdateStatusAsync(request.Id, request.IsActive);
        return result.ToActionResult();
    }
}

public class UpdateStatusRequest
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}