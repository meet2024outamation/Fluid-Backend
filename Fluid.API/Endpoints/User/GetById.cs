using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<UserResponse>
{
    private readonly IManageUserService _manageUserService;

    public GetById(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get user by ID",
        Description = "Retrieves a specific user by ID with role information using simplified response model",
        OperationId = "User.GetById",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "User found", typeof(UserResponse))]
    [SwaggerResponse(404, "User not found")]
    public async override Task<ActionResult<UserResponse>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.GetUserByIdAsync(id);
        return result.ToActionResult();
    }
}