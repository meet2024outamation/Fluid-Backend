using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Schema;

namespace Fluid.API.Endpoints.Schema;

[Route("api/schemas")]
public class Create : EndpointBaseAsync
    .WithRequest<CreateSchemaRequest>
    .WithActionResult<SchemaResponse>
{
    private readonly ISchemaService _schemaService;
    private readonly ICurrentUserService _currentUserService;

    public Create(ISchemaService schemaService, ICurrentUserService currentUserService)
    {
        _schemaService = schemaService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new schema",
        Description = "Creates a new schema with its associated schema fields",
        OperationId = "Schema.Create",
        Tags = new[] { "Schemas" })
    ]
    public async override Task<ActionResult<SchemaResponse>> HandleAsync(
        CreateSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _schemaService.CreateAsync(request, currentUserId);
        return result.ToActionResult();
    }
}