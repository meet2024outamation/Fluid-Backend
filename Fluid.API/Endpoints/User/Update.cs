using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

public class UpdateUserRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public UserRequest UserData { get; set; } = new UserRequest();
}

[Route("api/users")]
public class Update : EndpointBaseAsync
    .WithRequest<UpdateUserRequest>
    .WithActionResult<UserResponse>
{
    private readonly IManageUserService _manageUserService;

    public Update(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Update user by ID",
        Description = "Updates an existing user's information using simplified request/response models",
        OperationId = "User.Update",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(200, "User updated successfully", typeof(UserResponse))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(409, "Email already exists")]
    public async override Task<ActionResult<UserResponse>> HandleAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.UpdateUserAsync(request.Id, request.UserData);
        return result.ToActionResult();
    }
}