using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.Order;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.Order;

[Route("api/orders")]
public class GetById : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<OrderDto>
{
    private readonly IOrderService _orderService;

    public GetById(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get order by ID with documents",
        Description = "Retrieves a specific order with all its details including documents, order data, and document metadata such as file size, pages, and searchable content",
        OperationId = "Order.GetById",
        Tags = new[] { "Orders" })
    ]
    [SwaggerResponse(200, "Order retrieved successfully with document details", typeof(OrderDto))]
    [SwaggerResponse(404, "Order not found")]
    [SwaggerResponse(401, "Unauthorized")]
    public async override Task<ActionResult<OrderDto>> HandleAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrderByIdAsync(id);
        return result.ToActionResult();
    }
}