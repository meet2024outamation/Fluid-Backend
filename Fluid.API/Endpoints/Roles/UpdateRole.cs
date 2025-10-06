using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Role;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Roles;

[Route("api/roles")]
[ApiController]
public class UpdateRole : EndpointBaseAsync
    .WithRequest<UpdateRoleRequest>
    .WithActionResult<RoleDto>
{
    private readonly IRoleService _roleService;

    public UpdateRole(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpPut("{id:int}")]
    [AuthorizePermission(ApplicationPermissions.UpdateRoles)]
    [SwaggerOperation(
        Summary = "Update role",
        Description = "Updates an existing role name and resets assigned permissions. Requires UpdateRoles permission.",
        OperationId = "UpdateRole",
        Tags = new[] { "Roles" })]
    [SwaggerResponse(200, "Role updated successfully", typeof(RoleDto))]
    [SwaggerResponse(400, "Bad request - Invalid data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    [SwaggerResponse(404, "Role not found")]
    public override async Task<ActionResult<RoleDto>> HandleAsync([FromBody] UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get the ID from the route
        var routeValues = HttpContext.Request.RouteValues;
        if (!routeValues.TryGetValue("id", out var idValue) || !int.TryParse(idValue?.ToString(), out var id))
        {
            return BadRequest("Invalid role ID");
        }

        var result = await _roleService.UpdateRoleAsync(id, request);
        return result.ToActionResult();
    }
}