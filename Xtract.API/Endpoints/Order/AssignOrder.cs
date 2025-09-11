using Ardalis.ApiEndpoints;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Result.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Models.Order;

namespace Xtract.API.Endpoints.Order;

[Route("api/orders")]
public class AssignOrder : EndpointBaseAsync
    .WithRequest<AssignOrderRequest>
    .WithActionResult<AssignOrderResponse>
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public AssignOrder(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    [HttpPost("assign")]
    [SwaggerOperation(
        Summary = "Assign order to user",
        Description = "Assigns a specific order to a user. The order must be in an assignable status (Created, ReadyForAI, AICompleted, or ReadyForAssignment).",
        OperationId = "Order.Assign",
        Tags = new[] { "Orders" })
    ]
    public async override Task<ActionResult<AssignOrderResponse>> HandleAsync(
        [FromBody] AssignOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var result = await _orderService.AssignOrderAsync(request, currentUserId);
        return result.ToActionResult();
    }
}