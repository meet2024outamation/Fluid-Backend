using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Batch;

namespace Fluid.API.Endpoints.Batch;

public class UpdateStatusRequest
{
    [FromRoute]
    public int Id { get; set; }
    
    [FromBody]
    public UpdateBatchStatusRequest Request { get; set; } = null!;
}

[Route("api/batches")]
public class UpdateStatus : EndpointBaseAsync
    .WithRequest<UpdateStatusRequest>
    .WithActionResult<BatchResponse>
{
    private readonly IBatchService _batchService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateStatus(IBatchService batchService, ICurrentUserService currentUserService)
    {
        _batchService = batchService;
        _currentUserService = currentUserService;
    }

    [HttpPatch("{id:int}/status")]
    [SwaggerOperation(
        Summary = "Update batch status",
        Description = "Updates the status of a specific batch",
        OperationId = "Batch.UpdateStatus",
        Tags = new[] { "Batches" })
    ]
    public async override Task<ActionResult<BatchResponse>> HandleAsync(
        UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _batchService.UpdateStatusAsync(request.Id, request.Request, currentUserId);
        return result.ToActionResult();
    }
}