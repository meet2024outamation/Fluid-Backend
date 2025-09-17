using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using static Fluid.API.Models.User.UserParam;

namespace Fluid.API.Endpoints.User;

[Route("api/users")]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<UserEM>
{
    private readonly IManageUserService _manageUserService;

    public GetById(IManageUserService manageUserService)
    {
        _manageUserService = manageUserService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get user by ID",
        Description = "Retrieves a specific user by their ID",
        OperationId = "User.GetById",
        Tags = new[] { "Users" })
    ]
    public async override Task<ActionResult<UserEM>> HandleAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _manageUserService.GetUserById(id);
        return result.ToActionResult();
    }
}