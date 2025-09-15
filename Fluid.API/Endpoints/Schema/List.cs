using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Schema;

namespace Fluid.API.Endpoints.Schema;

[Route("api/schemas")]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<SchemaListResponse>>
{
    private readonly ISchemaService _schemaService;

    public List(ISchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all schemas",
        Description = "Retrieves a list of all schemas in the system, optionally filtered by client ID",
        OperationId = "Schema.List",
        Tags = new[] { "Schemas" })
    ]
    public async override Task<ActionResult<List<SchemaListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var clientId = HttpContext.Request.Query.ContainsKey("clientId") 
            ? int.TryParse(HttpContext.Request.Query["clientId"], out var id) ? (int?)id : null
            : null;

        var result = await _schemaService.GetAllAsync(clientId);
        return result.ToActionResult();
    }
}