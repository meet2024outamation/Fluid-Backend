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
public class Update : EndpointBaseAsync
    .WithRequest<UpdateGlobalSchemaRequestWithId>
    .WithActionResult<GlobalSchemaResponse>
{
    private readonly IGlobalSchemaService _globalSchemaService;
    private readonly ICurrentUserService _currentUserService;

    public Update(IGlobalSchemaService globalSchemaService, ICurrentUserService currentUserService)
    {
        _globalSchemaService = globalSchemaService;
        _currentUserService = currentUserService;
    }

    [HttpPut("{Id:int}")]
    [SwaggerOperation(
        Summary = "Update a global schema",
        Description = "Updates an existing global schema and its fields. Requires Product Owner role.",
        OperationId = "GlobalSchema.Update",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Global schema updated successfully", typeof(GlobalSchemaResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Global schema not found")]
    [SwaggerResponse(409, "Schema with the same name already exists")]
    public async override Task<ActionResult<GlobalSchemaResponse>> HandleAsync(
        UpdateGlobalSchemaRequestWithId request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId();
        var result = await _globalSchemaService.UpdateAsync(request.Id, request.UpdateRequest, userId);
        return result.ToActionResult();
    }
}

public class UpdateGlobalSchemaRequestWithId
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public CreateGlobalSchemaRequest UpdateRequest { get; set; } = new CreateGlobalSchemaRequest();
}