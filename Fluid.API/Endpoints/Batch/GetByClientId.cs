using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Batch;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Batch;

[Route("api/batches")]
public class GetByClientId : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<List<BatchListResponse>>
{
    private readonly IBatchService _batchService;

    public GetByClientId(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("client/{clientId:int}")]
    [SwaggerOperation(
        Summary = "Get batches by client ID",
        Description = "Retrieves all batches for a specific client",
        OperationId = "Batch.GetByClientId",
        Tags = new[] { "Batches" })
    ]
    public async override Task<ActionResult<List<BatchListResponse>>> HandleAsync(
        int clientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetByClientIdAsync(clientId);
        return result.ToActionResult();
    }
}