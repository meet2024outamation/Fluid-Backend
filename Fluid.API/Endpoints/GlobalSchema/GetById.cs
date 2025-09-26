using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.IAMSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.GlobalSchema;

[Route("api/global-schemas")]
[Authorize(Policy = AuthorizationPolicies.TenantAdminPolicy)]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<GlobalSchemaResponse>
{
    private readonly IGlobalSchemaService _globalSchemaService;

    public GetById(IGlobalSchemaService globalSchemaService)
    {
        _globalSchemaService = globalSchemaService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get global schema by ID",
        Description = "Retrieves a specific global schema with all its fields. Requires Product Owner role.",
        OperationId = "GlobalSchema.GetById",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Global schema retrieved successfully", typeof(GlobalSchemaResponse))]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Global schema not found")]
    public async override Task<ActionResult<GlobalSchemaResponse>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _globalSchemaService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}