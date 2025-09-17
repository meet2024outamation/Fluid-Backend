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
    [FromBody] public UserEM UserData { get; set; } = new UserEM();
}

[Route("api/users")]
public class Update : EndpointBaseAsync
    .WithRequest<UpdateUserRequest>
    .WithActionResult<UserCM>
{
    private readonly IManageUserService _manageUserService;

    public Update(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Update user by ID",
        Description = "Updates an existing user's information",
        OperationId = "User.Update",
        Tags = new[] { "Users" })
    ]
    public async override Task<ActionResult<UserCM>> HandleAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Set the ID from the route to the user data
        request.UserData.Id = request.Id;
        
        var result = await _manageUserService.UpdateUser(request.UserData);
        return result.ToActionResult();
    }
}