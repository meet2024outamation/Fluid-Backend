using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Schema;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Schema;

[Route("api/schemas")]
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateSchemaStatusEndpointRequest>
    .WithActionResult<SchemaResponse>
{
    private readonly ISchemaService _schemaService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateStatus(ISchemaService schemaService, ICurrentUserService currentUserService)
    {
        _schemaService = schemaService;
        _currentUserService = currentUserService;
    }

    [HttpPatch("{id}")]
    [SwaggerOperation(
        Summary = "Update schema status",
        Description = "Updates the active status of a schema by ID",
        OperationId = "Schema.UpdateStatus",
        Tags = new[] { "Schemas" })
    ]
    public async override Task<ActionResult<SchemaResponse>> HandleAsync(
        UpdateSchemaStatusEndpointRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _schemaService.UpdateStatusAsync(request.Id, request.StatusRequest, currentUserId);
        return result.ToActionResult();
    }
}

public class UpdateSchemaStatusEndpointRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public UpdateSchemaStatusRequest StatusRequest { get; set; } = new UpdateSchemaStatusRequest();
}