using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<UserListItem>>
{
    private readonly IManageUserService _manageUserService;

    public List(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all users",
        Description = "Retrieves a list of all users with optional filtering by active status using simplified list model",
        OperationId = "User.List",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "Users retrieved successfully", typeof(List<UserListItem>))]
    public async override Task<ActionResult<List<UserListItem>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        // You can also add query parameters for filtering
        var isActive = HttpContext.Request.Query.ContainsKey("isActive")
            ? bool.Parse(HttpContext.Request.Query["isActive"].ToString())
            : (bool?)null;

        var result = await _manageUserService.GetUsersAsync(isActive);
        return result.ToActionResult();
    }
}