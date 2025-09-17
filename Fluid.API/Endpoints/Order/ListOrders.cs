using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Order;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Order;

[Route("api/orders")]
public class ListOrders : EndpointBaseAsync
    .WithRequest<OrderListRequest>
    .WithActionResult<OrderListPagedResponse>
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public ListOrders(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get list of orders",
        Description = "Retrieves a paginated list of orders with filtering, searching, and sorting capabilities. Supports filtering by project, batch, status, assigned user, dates, priority, and validation errors.",
        OperationId = "Order.List",
        Tags = new[] { "Orders" })
    ]
    public async override Task<ActionResult<OrderListPagedResponse>> HandleAsync(
        [FromQuery] OrderListRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderService.GetOrdersAsync(request, currentUserId);
        return result.ToActionResult();
    }
}