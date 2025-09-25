using Ardalis.ApiEndpoints;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderFlow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.OrderFlow;

[Route("api/order-flows")]
[Authorize]
public class GetById : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<OrderFlowResponse>
{
    private readonly IOrderFlowService _orderFlowService;

    public GetById(IOrderFlowService orderFlowService)
    {
        _orderFlowService = orderFlowService;
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Get order flow by ID",
        Description = "Retrieves a specific order flow by its ID",
        OperationId = "OrderFlow.GetById",
        Tags = new[] { "OrderFlow" })
    ]
    [SwaggerResponse(200, "Order flow retrieved successfully", typeof(OrderFlowResponse))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order flow not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<OrderFlowResponse>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var id = int.Parse(HttpContext.Request.RouteValues["id"]?.ToString() ?? "0");
        var result = await _orderFlowService.GetByIdAsync(id);
        return result.ToActionResult();
    }
}