using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Schema;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Schema;

[Route("api/schemas")]
[AuthorizePermission(ApplicationPermissions.UpdateSchemas)]
public class Update : EndpointBaseAsync
    .WithRequest<UpdateSchemaEndpointRequest>
    .WithActionResult<SchemaResponse>
{
    private readonly ISchemaService _schemaService;
    private readonly ICurrentUserService _currentUserService;

    public Update(ISchemaService schemaService, ICurrentUserService currentUserService)
    {
        _schemaService = schemaService;
        _currentUserService = currentUserService;
    }

    [HttpPut("{Id}")]
    [SwaggerOperation(
        Summary = "Update schema by ID",
        Description = "Updates a schema and its schema fields by ID",
        OperationId = "Schema.Update",
        Tags = new[] { "Schemas" })
    ]
    public async override Task<ActionResult<SchemaResponse>> HandleAsync(
        UpdateSchemaEndpointRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _schemaService.UpdateAsync(request.Id, request.UpdateRequest, currentUserId);
        return result.ToActionResult();
    }
}

