using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Order;

namespace Fluid.API.Endpoints.Order;

[Route("api/orders")]
public class UpdateSchemaFieldValue : EndpointBaseAsync
    .WithRequest<UpdateSchemaFieldValueRequest>
    .WithActionResult<UpdateSchemaFieldValueResponse>
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateSchemaFieldValue(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    [HttpPut("field-value")]
    [SwaggerOperation(
        Summary = "Update schema field value manually",
        Description = "Allows operators to manually update any schema field value with audit logging. This is useful for correcting AI-extracted values or entering missing data.",
        OperationId = "Order.UpdateSchemaFieldValue",
        Tags = new[] { "Orders" })
    ]
    public async override Task<ActionResult<UpdateSchemaFieldValueResponse>> HandleAsync(
        [FromBody] UpdateSchemaFieldValueRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        
        // Get client IP and User Agent for audit logging
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        
        var result = await _orderService.UpdateSchemaFieldValueAsync(request, currentUserId, ipAddress, userAgent);
        return result.ToActionResult();
    }
}