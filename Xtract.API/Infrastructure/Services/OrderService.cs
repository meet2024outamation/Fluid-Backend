using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;
using Xtract.API.Infrastructure.Interfaces;
using Xtract.API.Models.Order;
using Xtract.Entities.Context;
using Xtract.Entities.Enums;

namespace Xtract.API.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly XtractDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(XtractDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<AssignOrderResponse>> AssignOrderAsync(AssignOrderRequest request, int currentUserId)
    {
        try
        {
            // Validate order exists
            var order = await _context.WorkItems
                .Include(o => o.AssignedUser)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderId),
                    ErrorMessage = "Order not found."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Validate user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.UserId),
                    ErrorMessage = "User not found."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if user is active
            if (!user.IsActive)
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.UserId),
                    ErrorMessage = "Cannot assign to inactive user."
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if order is in assignable status
            var assignableStatuses = new[]
            {
                WorkItemStatus.ReadyForAssignment
            };

            if (!assignableStatuses.Contains(order.Status))
            {
                var validationError = new ValidationError
                {
                    Key = nameof(request.OrderId),
                    ErrorMessage = $"Order cannot be assigned in current status: {order.Status}"
                };
                return Result<AssignOrderResponse>.Invalid(new List<ValidationError> { validationError });
            }

            // Assign the order
            order.AssignedTo = request.UserId;
            order.AssignedAt = DateTime.UtcNow;
            order.Status = WorkItemStatus.Assigned;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var response = new AssignOrderResponse
            {
                OrderId = order.Id,
                UserId = user.Id,
                UserName = user.Name,
                AssignedAt = order.AssignedAt.Value,
                Status = order.Status.ToString(),
                Message = "Order assigned successfully"
            };

            _logger.LogInformation("Order {OrderId} assigned to user {UserId} ({UserName})",
                order.Id, user.Id, user.Name);

            return Result<AssignOrderResponse>.Success(response, "Order assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning order {OrderId} to user {UserId}",
                request.OrderId, request.UserId);
            return Result<AssignOrderResponse>.Error("An error occurred while assigning the order.");
        }
    }
}