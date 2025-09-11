using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Models.Client;

namespace Xtract.API.Endpoints.Client;

[Route("api/clients")]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<ClientResponse>
{
    private readonly IClientService _clientService;

    public GetById(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get client by ID",
        Description = "Retrieves a specific client by their ID",
        OperationId = "Client.GetById",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<ClientResponse>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}