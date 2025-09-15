using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;

namespace Fluid.API.Endpoints.Client;

[Route("api/clients")]
public class Delete : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<bool>
{
    private readonly IClientService _clientService;

    public Delete(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Delete client by ID",
        Description = "Deletes a specific client by their ID. Cannot delete if client has associated batches or field mappings.",
        OperationId = "Client.Delete",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<bool>> HandleAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.DeleteAsync(id);
        return result.ToActionResult();
    }
}