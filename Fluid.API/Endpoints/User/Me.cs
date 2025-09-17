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
    .WithActionResult<UserEM>
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
        Description = "Retrieves the current authenticated user's information",
        OperationId = "User.Me",
        Tags = new[] { "Users" })
    ]
    public async override Task<ActionResult<UserEM>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUser.Id;
        var result = await _manageUserService.GetUserById(currentUserId);
        return result.ToActionResult();
    }
}