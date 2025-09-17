using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;

namespace Fluid.API.Endpoints.User;

public class UpdateStatusRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public UpdateUserStatusRequest StatusData { get; set; } = new UpdateUserStatusRequest();
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}

[Route("api/users")]
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateStatusRequest>
    .WithActionResult<int>
{
    private readonly IManageUserService _manageUserService;

    public UpdateStatus(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpPatch("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Update user status",
        Description = "Updates the active status of a user and syncs with Azure AD",
        OperationId = "User.UpdateStatus",
        Tags = new[] { "Users" })
    ]
    public async override Task<ActionResult<int>> HandleAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.UpdateUserStatusAsync(request.Id, request.StatusData.IsActive);
        return result.ToActionResult();
    }
}