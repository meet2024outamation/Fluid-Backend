using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.FieldMapping;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.FieldMapping;

[Route("api/field-mappings")]
public class GetByClient : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<List<FieldMappingResponse>>
{
    private readonly IFieldMappingService _fieldMappingService;

    public GetByClient(IFieldMappingService fieldMappingService)
    {
        _fieldMappingService = fieldMappingService;
    }

    [HttpGet("client/{clientId}")]
    [SwaggerOperation(
        Summary = "Get field mappings by client ID",
        Description = "Retrieves all field mappings for a specific client",
        OperationId = "SimpleFieldMapping.GetByClient",
        Tags = new[] { "Simple Field Mappings" })
    ]
    public async override Task<ActionResult<List<FieldMappingResponse>>> HandleAsync(
        [FromRoute] int clientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _fieldMappingService.GetByClientIdAsync(clientId);
        return result.ToActionResult();
    }
}