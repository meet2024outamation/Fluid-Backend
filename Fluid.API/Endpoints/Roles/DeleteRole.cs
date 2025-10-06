using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Roles;

[Route("api/roles")]
[ApiController]
public class DeleteRole : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<bool>
{
    private readonly IRoleService _roleService;

    public DeleteRole(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpDelete("{id:int}")]
    [AuthorizePermission(ApplicationPermissions.DeleteRoles)]
    [SwaggerOperation(
        Summary = "Delete role",
        Description = "Soft deletes a role and removes all role-permission assignments. Cannot delete roles assigned to users. Requires DeleteRoles permission.",
        OperationId = "DeleteRole",
        Tags = new[] { "Roles" })]
    [SwaggerResponse(204, "Role deleted successfully")]
    [SwaggerResponse(400, "Bad request - Role is assigned to users")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    [SwaggerResponse(404, "Role not found")]
    public override async Task<ActionResult<bool>> HandleAsync([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var result = await _roleService.DeleteRoleAsync(id);
        return result.ToActionResult();
    }
}