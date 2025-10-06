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
[Authorize(Policy = AuthorizationPolicies.TenantAdminPolicy)]
public class CopyToTenant : EndpointBaseAsync
    .WithRequest<CopySchemaToTenantRequest>
    .WithActionResult<CopySchemaToTenantResponse>
{
    private readonly IGlobalSchemaService _globalSchemaService;
    private readonly ICurrentUserService _currentUserService;

    public CopyToTenant(IGlobalSchemaService globalSchemaService, ICurrentUserService currentUserService)
    {
        _globalSchemaService = globalSchemaService;
        _currentUserService = currentUserService;
    }

    [HttpPost("copy-to-tenant")]
    [SwaggerOperation(
        Summary = "Copy global schema to tenant",
        Description = "Copies a global schema to a specific tenant's database. Requires Product Owner role.",
        OperationId = "GlobalSchema.CopyToTenant",
        Tags = new[] { "Global Schemas" })
    ]
    [SwaggerResponse(200, "Schema copied to tenant successfully", typeof(CopySchemaToTenantResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized - User not authenticated")]
    [SwaggerResponse(403, "Forbidden - User does not have Product Owner role")]
    [SwaggerResponse(404, "Global schema or tenant not found")]
    [SwaggerResponse(409, "Schema with the same name already exists in tenant")]
    public async override Task<ActionResult<CopySchemaToTenantResponse>> HandleAsync(
        CopySchemaToTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetCurrentUserId();
        var result = await _globalSchemaService.CopySchemaToTenantAsync(request, userId);
        return result.ToActionResult();
    }
}