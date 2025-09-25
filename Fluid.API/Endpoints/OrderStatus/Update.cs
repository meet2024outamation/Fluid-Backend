using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.OrderStatus;

[Route("api/order-statuses")]
[Authorize]
public class Update : EndpointBaseAsync
    .WithRequest<UpdateOrderStatusRequest>
    .WithActionResult<OrderStatusResponse>
{
    private readonly IOrderStatusService _orderStatusService;
    private readonly ICurrentUserService _currentUserService;

    public Update(IOrderStatusService orderStatusService, ICurrentUserService currentUserService)
    {
        _orderStatusService = orderStatusService;
        _currentUserService = currentUserService;
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Update an order status",
        Description = "Updates an existing order status",
        OperationId = "OrderStatus.Update",
        Tags = new[] { "OrderStatus" })
    ]
    [SwaggerResponse(200, "Order status updated successfully", typeof(OrderStatusResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order status not found")]
    [SwaggerResponse(409, "Order status with the same name or display order already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<OrderStatusResponse>> HandleAsync(
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = int.Parse(HttpContext.Request.RouteValues["id"]?.ToString() ?? "0");
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderStatusService.UpdateAsync(id, request, currentUserId);
        return result.ToActionResult();
    }
}