using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Batch;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Batch;

[Route("api/batches")]
public class GetByProjectId : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<List<BatchListResponse>>
{
    private readonly IBatchService _batchService;

    public GetByProjectId(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("project/{projectId:int}")]
    [SwaggerOperation(
        Summary = "Get batches by project ID",
        Description = "Retrieves all batches for a specific project",
        OperationId = "Batch.GetByProjectId",
        Tags = new[] { "Batches" })
    ]
    public async override Task<ActionResult<List<BatchListResponse>>> HandleAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetByProjectIdAsync(projectId);
        return result.ToActionResult();
    }
}