using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.FieldMapping;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.FieldMapping;

[Route("api/field-mappings")]
public class CreateBulkMappings : EndpointBaseAsync
    .WithRequest<CreateBulkFieldMappingRequest>
    .WithActionResult<BulkFieldMappingResponse>
{
    private readonly IFieldMappingService _fieldMappingService;
    private readonly ICurrentUserService _currentUserService;

    public CreateBulkMappings(IFieldMappingService fieldMappingService, ICurrentUserService currentUserService)
    {
        _fieldMappingService = fieldMappingService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create multiple field mappings",
        Description = "Creates multiple field mappings for a project and schema in a single transaction. This will replace all existing mappings for the project and schema.",
        OperationId = "SimpleFieldMapping.CreateBulk",
        Tags = new[] { "Simple Field Mappings" })
    ]
    public async override Task<ActionResult<BulkFieldMappingResponse>> HandleAsync(
        CreateBulkFieldMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _fieldMappingService.CreateBulkFieldMappingsAsync(request, currentUserId);
        return result.ToActionResult();
    }
}