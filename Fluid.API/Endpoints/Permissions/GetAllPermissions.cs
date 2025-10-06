using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Role;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Permissions;

[Route("api/permissions")]
[ApiController]
public class GetAllPermissions : EndpointBaseAsync.WithoutRequest.WithActionResult<List<PermissionDto>>
{
    private readonly IRoleService _roleService;

    public GetAllPermissions(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [AuthorizePermission(ApplicationPermissions.ViewPermissions)]
    [SwaggerOperation(
        Summary = "Get all permissions",
        Description = "Retrieves all available permissions in the system. Requires ViewPermissions permission.",
        OperationId = "GetAllPermissions",
        Tags = new[] { "Permissions" })]
    [SwaggerResponse(200, "Success", typeof(List<PermissionDto>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    public override async Task<ActionResult<List<PermissionDto>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var result = await _roleService.GetAllPermissionsAsync();

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(result.Errors);
    }
}