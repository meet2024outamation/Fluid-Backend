using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderFlow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.OrderFlow;

[Route("api/order-flows")]
[Authorize]
[RequireTenantAccess]
public class Create : EndpointBaseAsync
    .WithRequest<CreateOrderFlowRequest>
    .WithActionResult<List<OrderFlowResponse>>
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
        Summary = "Create a new order flow (multiple steps)",
        Description = "Creates a new order flow with multiple steps in a single request",
        OperationId = "OrderFlow.Create",
        Tags = new[] { "OrderFlow" })
    ]
    [SwaggerResponse(201, "Order flow steps created successfully", typeof(List<OrderFlowResponse>))]
    [SwaggerResponse(400, "Invalid request data")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Order not found")]
    [SwaggerResponse(409, "Order flow with the same rank already exists for this order")]
    [SwaggerResponse(500, "Internal server error")]
    public async override Task<ActionResult<List<OrderFlowResponse>>> HandleAsync(
        CreateOrderFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderFlowService.CreateFlowAsync(request.Steps, currentUserId);
        return result.ToActionResult();
    }
}