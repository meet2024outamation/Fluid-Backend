using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class Create : EndpointBaseAsync
    .WithRequest<UserCM>
    .WithActionResult<UserEM>
{
    private readonly IManageUserService _manageUserService;

    public Create(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new user",
        Description = "Creates a new user with Azure AD integration and role assignment",
        OperationId = "User.Create",
        Tags = new[] { "Users" })
    ]
    public async override Task<ActionResult<UserEM>> HandleAsync(
        UserCM request,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.CreateUser(request);
        return result.ToActionResult();
    }
}