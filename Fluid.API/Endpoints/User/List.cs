using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<IEnumerable<UserList>>
{
    private readonly IManageUserService _manageUserService;

    public List(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all users",
        Description = "Retrieves a list of all active users in the system",
        OperationId = "User.List",
        Tags = new[] { "Users" })
    ]
    public async override Task<ActionResult<IEnumerable<UserList>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.ListUsersAsync();
        return Ok(result);
    }
}