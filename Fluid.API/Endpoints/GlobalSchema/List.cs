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
[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<GlobalSchemaListResponse>>
{
    private readonly IGlobalSchemaService _globalSchemaService;

    public List(IGlobalSchemaService globalSchemaService)
    {
        _globalSchemaService = globalSchemaService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all global schemas",
        Description = "Retrieves all global schemas from IAM database. Requires Product Owner role.",
        OperationId = "GlobalSchema.List",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Global schemas retrieved successfully", typeof(List<GlobalSchemaListResponse>))]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    public async override Task<ActionResult<List<GlobalSchemaListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var isActive = HttpContext.Request.Query.ContainsKey("isActive") 
            ? bool.Parse(HttpContext.Request.Query["isActive"]!) 
            : (bool?)null;

        var result = await _globalSchemaService.GetAllAsync(isActive);
        return result.ToActionResult();
    }
}