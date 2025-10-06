using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Order;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Order;

[Route("api/orders")]
public class List : EndpointBaseAsync
    .WithRequest<OrderListRequest>
    .WithActionResult<OrderListPagedResponse>
{
    private readonly IOrderService _orderService;

    public List(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get orders with filtering and pagination",
        Description = "Retrieves a paginated list of orders with optional filtering by project, batch, status, assigned user, and more",
        OperationId = "Order.List",
        Tags = new[] { "Orders" })
    ]
    [SwaggerResponse(200, "Orders retrieved successfully", typeof(OrderListPagedResponse))]
    [SwaggerResponse(400, "Bad request - Invalid parameters")]
    [SwaggerResponse(401, "Unauthorized")]
    public async override Task<ActionResult<OrderListPagedResponse>> HandleAsync(
        [FromQuery] OrderListRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersAsync(request);
        return result.ToActionResult();
    }
}