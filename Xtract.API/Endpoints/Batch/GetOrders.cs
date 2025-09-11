using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Models.Batch;

namespace Xtract.API.Endpoints.Batch;

[Route("api/batches")]
public class GetOrders : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<List<BatchOrderResponse>>
{
    private readonly IBatchService _batchService;

    public GetOrders(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("{id:int}/orders")]
    [SwaggerOperation(
        Summary = "Get batch orders",
        Description = "Retrieves all orders for a specific batch with their validation status",
        OperationId = "Batch.GetOrders",
        Tags = new[] { "Batches" })
    ]
    public async override Task<ActionResult<List<BatchOrderResponse>>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetBatchOrdersAsync(id);
        return result.ToActionResult();
    }
}