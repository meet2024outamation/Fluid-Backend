using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Schema;

[Route("api/schemas")]
public class Delete : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<bool>
{
    private readonly ISchemaService _schemaService;

    public Delete(ISchemaService schemaService)
    {
        _schemaService = schemaService;
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "Delete schema by ID",
        Description = "Deletes a schema and all its schema fields by ID. Cannot delete if schema is being used by field mappings.",
        OperationId = "Schema.Delete",
        Tags = new[] { "Schemas" })
    ]
    public async override Task<ActionResult<bool>> HandleAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _schemaService.DeleteAsync(id);
        return result.ToActionResult();
    }
}