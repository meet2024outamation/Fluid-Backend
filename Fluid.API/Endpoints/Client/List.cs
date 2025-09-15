using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Client;

namespace Fluid.API.Endpoints.Client;

[Route("api/clients")]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<ClientListResponse>>
{
    private readonly IClientService _clientService;

    public List(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all clients",
        Description = "Retrieves a list of all clients in the system",
        OperationId = "Client.List",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<List<ClientListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.GetAllAsync();
        return result.ToActionResult();
    }
}