using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Batch;
using Fluid.API.Authorization;

namespace Fluid.API.Endpoints.Batch;

[Route("api/batches")]
[RequireTenantAccess]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<BatchResponse>
{
    private readonly IBatchService _batchService;

    public GetById(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get batch by ID",
        Description = "Retrieves a specific batch by ID with detailed information including validation results",
        OperationId = "Batch.GetById",
        Tags = new[] { "Batches" })
    ]
    public async override Task<ActionResult<BatchResponse>> HandleAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}