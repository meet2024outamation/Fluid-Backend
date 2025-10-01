using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Batch;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Batch;

[Route("api/batches")]
[RequireTenantAccess]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<BatchListResponse>>
{
    private readonly IBatchService _batchService;

    public List(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all batches",
        Description = "Retrieves a list of all batches in the system with summary information",
        OperationId = "Batch.List",
        Tags = new[] { "Batches" })
    ]
    public async override Task<ActionResult<List<BatchListResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _batchService.GetAllAsync();
        return result.ToActionResult();
    }
}