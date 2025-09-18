using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

public class UpdateStatusRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public UserStatusRequest StatusData { get; set; } = new UserStatusRequest();
}

[Route("api/users")]
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateStatusRequest>
    .WithActionResult<bool>
{
    private readonly IManageUserService _manageUserService;

    public UpdateStatus(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpPatch("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Update user status",
        Description = "Updates the active status of a user and syncs with Azure AD using simplified request model",
        OperationId = "User.UpdateStatus",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "User status updated successfully")]
    [SwaggerResponse(404, "User not found")]
    public async override Task<ActionResult<bool>> HandleAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.UpdateUserStatusAsync(request.Id, request.StatusData.IsActive);
        return result.ToActionResult();
    }
}