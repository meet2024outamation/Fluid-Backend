using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class Delete : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<bool>
{
    private readonly IManageUserService _manageUserService;

    public Delete(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Delete user",
        Description = "Soft deletes a user by marking them as inactive",
        OperationId = "User.Delete",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "User deleted successfully")]
    [SwaggerResponse(404, "User not found")]
    public async override Task<ActionResult<bool>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.DeleteUserAsync(id);
        return result.ToActionResult();
    }
}