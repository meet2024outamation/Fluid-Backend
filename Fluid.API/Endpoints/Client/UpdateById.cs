using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Client;

namespace Fluid.API.Endpoints.Client;

public class UpdateByIdRequest
{
    [FromRoute]
    public int Id { get; set; }
    
    [FromBody]
    public UpdateClientRequest Request { get; set; } = null!;
}

[Route("api/clients")]
public class UpdateById : EndpointBaseAsync
    .WithRequest<UpdateByIdRequest>
    .WithActionResult<ClientResponse>
{
    private readonly IClientService _clientService;

    public UpdateById(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Update client by ID",
        Description = "Updates a specific client by their ID",
        OperationId = "Client.UpdateById",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<ClientResponse>> HandleAsync(
        UpdateByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.UpdateAsync(request.Id, request.Request);
        return result.ToActionResult();
    }
}