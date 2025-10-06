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
public class CreateRole : EndpointBaseAsync
    .WithRequest<CreateRoleRequest>
    .WithActionResult<RoleDto>
{
    private readonly IRoleService _roleService;

    public CreateRole(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpPost]
    [AuthorizePermission(ApplicationPermissions.CreateRoles)]
    [SwaggerOperation(
        Summary = "Create new role",
        Description = "Creates a new role with specified permissions. Requires CreateRoles permission.",
        OperationId = "CreateRole",
        Tags = new[] { "Roles" })]
    [SwaggerResponse(201, "Role created successfully", typeof(RoleDto))]
    [SwaggerResponse(400, "Bad request - Invalid data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(403, "Forbidden - Missing required permission")]
    public override async Task<ActionResult<RoleDto>> HandleAsync([FromBody] CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _roleService.CreateRoleAsync(request);
        return result.ToActionResult();
    }
}