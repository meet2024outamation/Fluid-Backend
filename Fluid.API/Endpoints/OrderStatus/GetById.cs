using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.OrderStatus;

[Route("api/order-statuses")]
[Authorize]
[RequireTenantAccess]
public class GetById : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<OrderStatusResponse>
{
    private readonly IOrderStatusService _orderStatusService;

    public GetById(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get order status by ID",
        Description = "Retrieves a specific order status by its ID",
        OperationId = "OrderStatus.GetById",
        Tags = new[] { "OrderStatus" })
    ]
    [SwaggerResponse(200, "Order status retrieved successfully", typeof(OrderStatusResponse))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order status not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<OrderStatusResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var id = int.Parse(HttpContext.Request.RouteValues["id"]?.ToString() ?? "0");
        var result = await _orderStatusService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}