using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using SharedKernel.Services;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class Me : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<UserMeResponse>
{
    private readonly IManageUserService _manageUserService;
    private readonly IUser _currentUser;

    public Me(IManageUserService manageUserService, IUser currentUser)
    {
        _manageUserService = manageUserService;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get current user information",
        Description = "Retrieves the current authenticated user's information with simplified role details",
        OperationId = "User.Me",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "Current user information", typeof(UserMeResponse))]
    [SwaggerResponse(404, "User not found")]
    public async override Task<ActionResult<UserMeResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUser.Id;
        var result = await _manageUserService.GetCurrentUserAsync(currentUserId);
        return result.ToActionResult();
    }
}