using Ardalis.ApiEndpoints;
using Fluid.API.Authorization;
using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderFlow;
using Fluid.API.Models.OrderStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Fluid.API.Endpoints.OrderFlow;

[Route("api/order-flows")]
[Authorize]
[RequireTenantAccess]
public class List : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<OrderFlowResponse>>
{
    private readonly IOrderFlowService _orderFlowService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly ILogger<List> _logger;

    public List(IOrderFlowService orderFlowService, IOrderStatusService orderStatusService, ILogger<List> logger)
    {
        _orderFlowService = orderFlowService;
        _orderStatusService = orderStatusService;
        _logger = logger;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all order flows or fallback to order statuses",
        Description = "Returns all order flows for the tenant, or all order statuses if no order flows exist.",
        OperationId = "OrderFlow.List",
        Tags = new[] { "OrderFlow" })]
    [SwaggerResponse(200, "Order flows or order statuses returned", typeof(List<OrderFlowResponse>))]
    public async override Task<ActionResult<List<OrderFlowResponse>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        // Try to get all order flows for the tenant
        var flowsResult = await _orderFlowService.GetAllAsync();
        if (flowsResult.IsSuccess && flowsResult.Value != null && flowsResult.Value.Count > 0)
        {
            return flowsResult.ToActionResult();
        }

        // If no order flows, fallback to order statuses
        var statusesResult = await _orderStatusService.GetAllAsync();
        if (!statusesResult.IsSuccess || statusesResult.Value == null)
        {
            _logger.LogWarning("No order flows or order statuses found for tenant");
            return NotFound();
        }

        // Map order statuses to order flow response model
        var mapped = statusesResult.Value.Select((status, idx) => new OrderFlowResponse
        {
            Id = 0, // Not from DB
            OrderStatusId = status.Id,
            StatusName = status.Name,
            Rank = idx + 1, // Default rank order
            IsActive = status.IsActive,
            CreatedBy = 0,
            UpdatedBy = null,
            CreatedAt = status.CreatedAt,
            UpdatedAt = status.UpdatedAt
        }).ToList();
        return Ok(mapped);
    }
}
