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
public class Create : EndpointBaseAsync
    .WithRequest<CreateOrderStatusRequest>
    .WithActionResult<OrderStatusResponse>
{
    private readonly IOrderStatusService _orderStatusService;
    private readonly ICurrentUserService _currentUserService;

    public Create(IOrderStatusService orderStatusService, ICurrentUserService currentUserService)
    {
        _orderStatusService = orderStatusService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new order status",
        Description = "Creates a new order status in the system",
        OperationId = "OrderStatus.Create",
        Tags = new[] { "OrderStatus" })
    ]
    [SwaggerResponse(201, "Order status created successfully", typeof(OrderStatusResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(409, "Order status with the same name or display order already exists")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<OrderStatusResponse>> HandleAsync(
        CreateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderStatusService.CreateAsync(request, currentUserId);
        return result.ToActionResult();
    }
}