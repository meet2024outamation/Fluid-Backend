using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class Create : EndpointBaseAsync
    .WithRequest<UserRequest>
    .WithActionResult<UserResponse>
{
    private readonly IManageUserService _manageUserService;

    public Create(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new user",
        Description = "Creates a new user with Azure AD integration and role assignment using simplified request/response models",
        OperationId = "User.Create",
        Tags = new[] { "Users" })
    ]
    [SwaggerResponse(201, "User created successfully", typeof(UserResponse))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(409, "Email already exists")]
    public async override Task<ActionResult<UserResponse>> HandleAsync(
        UserRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.CreateUserAsync(request);
        return result.ToActionResult();
    }
}