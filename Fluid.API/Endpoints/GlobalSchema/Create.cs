using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.IAMSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using SharedKernel.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.GlobalSchema;

[Route("api/global-schemas")]
[Authorize(Policy = AuthorizationPolicies.ProductOwnerPolicy)]
public class Create : EndpointBaseAsync
    .WithRequest<CreateGlobalSchemaRequest>
    .WithActionResult<GlobalSchemaResponse>
{
    private readonly IGlobalSchemaService _globalSchemaService;
    private readonly IUser _currentUser;

    public Create(IGlobalSchemaService globalSchemaService, IUser currentUser)
    {
        _globalSchemaService = globalSchemaService;
        _currentUser = currentUser;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new global schema",
        Description = "Creates a new global schema in IAM database that can be reused across tenants and projects. Requires Product Owner role.",
        OperationId = "GlobalSchema.Create",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(201, "Global schema created successfully", typeof(GlobalSchemaResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(409, "Schema with the same name already exists")]
    public async override Task<ActionResult<GlobalSchemaResponse>> HandleAsync(
        CreateGlobalSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _globalSchemaService.CreateAsync(request, _currentUser.Id);
        return result.ToActionResult();
    }
}