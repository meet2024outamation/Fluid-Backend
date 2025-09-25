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
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<OrderStatusResponse>>
{
    private readonly IOrderStatusService _orderStatusService;

    public List(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all order statuses",
        Description = "Retrieves all order statuses ordered by display order",
        OperationId = "OrderStatus.List",
        Tags = new[] { "OrderStatus" })
    ]
    [SwaggerResponse(200, "Order statuses retrieved successfully", typeof(List<OrderStatusResponse>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<List<OrderStatusResponse>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _orderStatusService.GetAllAsync();
        return result.ToActionResult();
    }
}