using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.FieldMapping;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.FieldMapping;

[Route("api/field-mappings")]
public class GetByProject : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<List<FieldMappingResponse>>
{
    private readonly IFieldMappingService _fieldMappingService;

    public GetByProject(IFieldMappingService fieldMappingService)
    {
        _fieldMappingService = fieldMappingService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get field mappings by project ID",
        Description = "Retrieves all field mappings for a specific project",
        OperationId = "SimpleFieldMapping.GetByProject",
        Tags = new[] { "Simple Field Mappings" })
    ]
    public async override Task<ActionResult<List<FieldMappingResponse>>> HandleAsync(
        [FromQuery] int projectId,
        CancellationToken cancellationToken = default)
    {
        var result = await _fieldMappingService.GetByProjectIdAsync(projectId);
        return result.ToActionResult();
    }
}