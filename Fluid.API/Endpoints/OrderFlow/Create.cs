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
public class Create : EndpointBaseAsync
    .WithRequest<CreateOrderFlowRequest>
    .WithActionResult<OrderFlowResponse>
{
    private readonly IOrderFlowService _orderFlowService;
    private readonly ICurrentUserService _currentUserService;

    public Create(IOrderFlowService orderFlowService, ICurrentUserService currentUserService)
    {
        _orderFlowService = orderFlowService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new order flow",
        Description = "Creates a new order flow entry for tracking order status progression",
        OperationId = "OrderFlow.Create",
        Tags = new[] { "OrderFlow" })
    ]
    [SwaggerResponse(201, "Order flow created successfully", typeof(OrderFlowResponse))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order not found")]
    [SwaggerResponse(409, "Order flow with the same rank already exists for this order")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<OrderFlowResponse>> HandleAsync(
        CreateOrderFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderFlowService.CreateAsync(request, currentUserId);
        return result.ToActionResult();
    }
}