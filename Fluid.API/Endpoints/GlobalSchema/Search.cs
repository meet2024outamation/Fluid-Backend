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
public class Search : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<GlobalSchemaListResponse>>
{
    private readonly IGlobalSchemaService _globalSchemaService;

    public Search(IGlobalSchemaService globalSchemaService)
    {
        _globalSchemaService = globalSchemaService;
    }

    [HttpGet("search")]
    [SwaggerOperation(
        Summary = "Search global schemas",
        Description = "Searches global schemas by name or description. Requires Product Owner role.",
        OperationId = "GlobalSchema.Search",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Search results retrieved successfully", typeof(List<GlobalSchemaListResponse>))]
    [SwaggerResponse(400, "Search term is required")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    public async override Task<ActionResult<List<GlobalSchemaListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var searchTerm = HttpContext.Request.Query["q"].ToString();
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest("Search term 'q' is required");
        }

        var isActive = HttpContext.Request.Query.ContainsKey("isActive") 
            ? bool.Parse(HttpContext.Request.Query["isActive"]!) 
            : (bool?)null;

        var result = await _globalSchemaService.SearchAsync(searchTerm, isActive);
        return result.ToActionResult();
    }
}