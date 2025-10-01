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
public class Create : EndpointBaseAsync
    .WithRequest<CreateBatchRequest>
    .WithActionResult<BatchResponse>
{
    private readonly IBatchService _batchService;
    private readonly ICurrentUserService _currentUserService;

    public Create(IBatchService batchService, ICurrentUserService currentUserService)
    {
        _batchService = batchService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new batch",
        Description = "Creates a new batch from uploaded metadata file and optional documents. The system will process the metadata file, create orders for each record, and perform basic validations.",
        OperationId = "Batch.Create",
        Tags = new[] { "Batches" })
    ]
    [Consumes("multipart/form-data")]
    public async override Task<ActionResult<BatchResponse>> HandleAsync(
        [FromForm] CreateBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _batchService.CreateAsync(request, currentUserId);
        return result.ToActionResult();
    }
}