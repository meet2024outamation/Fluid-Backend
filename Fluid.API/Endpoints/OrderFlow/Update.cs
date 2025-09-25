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
public class Update : EndpointBaseAsync
    .WithRequest<UpdateOrderFlowRequest>
    .WithActionResult<OrderFlowResponse>
{
    private readonly IOrderFlowService _orderFlowService;
    private readonly ICurrentUserService _currentUserService;

    public Update(IOrderFlowService orderFlowService, ICurrentUserService currentUserService)
    {
        _orderFlowService = orderFlowService;
        _currentUserService = currentUserService;
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Update an order flow",
        Description = "Updates an existing order flow entry",
        OperationId = "OrderFlow.Update",
        Tags = new[] { "OrderFlow" })
    ]
    [SwaggerResponse(200, "Order flow updated successfully", typeof(OrderFlowResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order flow not found")]
    [SwaggerResponse(409, "Order flow with the same rank already exists for this order")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<OrderFlowResponse>> HandleAsync(
        [FromBody] UpdateOrderFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = int.Parse(HttpContext.Request.RouteValues["id"]?.ToString() ?? "0");
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderFlowService.UpdateAsync(id, request, currentUserId);
        return result.ToActionResult();
    }
}