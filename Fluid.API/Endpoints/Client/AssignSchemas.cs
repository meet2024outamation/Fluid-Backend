using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Client;

namespace Fluid.API.Endpoints.Client;

[Route("api/clients")]
public class AssignSchemas : EndpointBaseAsync
    .WithRequest<AssignSchemasRequest>
    .WithActionResult<ClientSchemaAssignmentResponse>
{
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;

    public AssignSchemas(IClientService clientService, ICurrentUserService currentUserService)
    {
        _clientService = clientService;
        _currentUserService = currentUserService;
    }

    [HttpPost("assign-schemas")]
    [SwaggerOperation(
        Summary = "Assign schemas to a client",
        Description = "Assigns a list of schemas to a specific client. This will replace any existing schema assignments for the client.",
        OperationId = "Client.AssignSchemas",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<ClientSchemaAssignmentResponse>> HandleAsync(
        AssignSchemasRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _clientService.AssignSchemasAsync(request, currentUserId);
        return result.ToActionResult();
    }
}