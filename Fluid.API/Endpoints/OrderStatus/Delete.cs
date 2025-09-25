using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.OrderStatus;

[Route("api/order-statuses")]
[Authorize]
public class Delete : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<bool>
{
    private readonly IOrderStatusService _orderStatusService;

    public Delete(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Delete order status",
        Description = "Deletes an order status if it has no associated orders or order flows",
        OperationId = "OrderStatus.Delete",
        Tags = new[] { "OrderStatus" })
    ]
    [SwaggerResponse(200, "Order status deleted successfully")]
    [SwaggerResponse(400, "Order status has associated orders or order flows")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order status not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<bool>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var id = int.Parse(HttpContext.Request.RouteValues["id"]?.ToString() ?? "0");
        var result = await _orderStatusService.DeleteAsync(id);
        return result.ToActionResult();
    }
}