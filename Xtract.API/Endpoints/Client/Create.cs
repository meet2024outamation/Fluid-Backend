using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Models.Client;

namespace Xtract.API.Endpoints.Client;

[Route("api/clients")]
public class Create : EndpointBaseAsync
    .WithRequest<CreateClientRequest>
    .WithActionResult<ClientResponse>
{
    private readonly IClientService _clientService;
    private readonly ICurrentUserService _currentUserService;

    public Create(IClientService clientService, ICurrentUserService currentUserService)
    {
        _clientService = clientService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new client",
        Description = "Creates a new client in the system",
        OperationId = "Client.Create",
        Tags = new[] { "Clients" })
    ]
    public async override Task<ActionResult<ClientResponse>> HandleAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _clientService.CreateAsync(request, currentUserId);
        return result.ToActionResult();
    }
}