using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Client;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Client;

public class UpdateStatusRequest
{
    [FromRoute]
    public int Id { get; set; }

    [FromBody]
    public UpdateClientStatusRequest Request { get; set; } = new UpdateClientStatusRequest();
}

[Route("api/clients")]
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateStatusRequest>
    .WithActionResult<ClientResponse>
{
    private readonly IClientService _clientService;

    public UpdateStatus(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpPatch("{id:int}")]
    [SwaggerOperation(
        Summary = "Update client status",
        Description = "Updates the active status of a specific client by their ID",
        OperationId = "Client.UpdateStatus",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<ClientResponse>> HandleAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.UpdateStatusAsync(request.Id, request.Request);
        return result.ToActionResult();
    }
}