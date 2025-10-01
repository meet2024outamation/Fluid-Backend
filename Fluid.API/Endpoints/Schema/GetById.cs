using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Schema;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Schema;

[Route("api/schemas")]
[RequireTenantAccess]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<SchemaResponse>
{
    private readonly ISchemaService _schemaService;

    public GetById(ISchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get schema by ID",
        Description = "Retrieves a schema by its ID with complete details including all schema fields",
        OperationId = "Schema.GetById",
        Tags = new[] { "Schemas" })
    ]
    public async override Task<ActionResult<SchemaResponse>> HandleAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _schemaService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}