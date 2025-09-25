using Fluid.API.Infrastructure.Interfaces;
using Fluid.API.Models.OrderStatus;
using Fluid.Entities.Context;
using Fluid.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Result;

namespace Fluid.API.Infrastructure.Services;

public class OrderStatusService : IOrderStatusService
{
    private readonly FluidDbContext _context;
    private readonly ILogger<OrderStatusService> _logger;

    public OrderStatusService(FluidDbContext context, ILogger<OrderStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<OrderStatusResponse>> CreateAsync(CreateOrderStatusRequest request, int currentUserId)
    {
        try
        {
            // Check if order status name already exists
            var existingOrderStatus = await _context.Set<OrderStatus>()
                .FirstOrDefaultAsync(os => os.Name == request.Name);

            if (existingOrderStatus != null)
            {
                _logger.LogWarning("Attempted to create order status with existing name: {Name}", request.Name);

                var validationError = new ValidationError
                {
                    Key = nameof(request.Name),
                    ErrorMessage = $"Order status with name '{request.Name}' already exists."
                };

                return Result<OrderStatusResponse>.Invalid(new List<ValidationError> { validationError });
            }

            var orderStatus = new OrderStatus
            {
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<OrderStatus>().Add(orderStatus);
            await _context.SaveChangesAsync();

            var response = new OrderStatusResponse
            {
                Id = orderStatus.Id,
                Name = orderStatus.Name,
                Description = orderStatus.Description,
                IsActive = orderStatus.IsActive,
                CreatedAt = orderStatus.CreatedAt,
                UpdatedAt = orderStatus.UpdatedAt,
                CreatedBy = orderStatus.CreatedBy,
                UpdatedBy = orderStatus.UpdatedBy,
                OrderCount = 0,
                OrderFlowCount = 0
            };

            _logger.LogInformation("Order status created successfully with ID: {OrderStatusId}", orderStatus.Id);
            return Result<OrderStatusResponse>.Created(response, "Order status created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order status with name: {Name}", request.Name);
            return Result<OrderStatusResponse>.Error("An error occurred while creating the order status.");
        }
    }

    public async Task<Result<OrderStatusResponse>> UpdateAsync(int id, UpdateOrderStatusRequest request, int currentUserId)
    {
        try
        {
            var orderStatus = await _context.Set<OrderStatus>()
                .FirstOrDefaultAsync(os => os.Id == id);

            if (orderStatus == null)
            {
                _logger.LogWarning("Order status with ID {OrderStatusId} not found for update", id);
                return Result<OrderStatusResponse>.NotFound();
            }

            // Check if the new name conflicts with another order status
            if (request.Name != orderStatus.Name)
            {
                var existingOrderStatus = await _context.Set<OrderStatus>()
                    .FirstOrDefaultAsync(os => os.Name == request.Name && os.Id != id);

                if (existingOrderStatus != null)
                {
                    _logger.LogWarning("Attempted to update order status {OrderStatusId} with existing name: {Name}", id, request.Name);

                    var validationError = new ValidationError
                    {
                        Key = nameof(request.Name),
                        ErrorMessage = $"Order status with name '{request.Name}' already exists."
                    };

                    return Result<OrderStatusResponse>.Invalid(new List<ValidationError> { validationError });
                }
            }

            // Update the order status properties
            orderStatus.Name = request.Name;
            orderStatus.Description = request.Description;
            orderStatus.IsActive = request.IsActive;
            orderStatus.UpdatedBy = currentUserId;
            orderStatus.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Get counts
            var orderCount = await _context.Orders.CountAsync(o => o.OrderStatusId == id);
            var orderFlowCount = await _context.Set<OrderFlow>().CountAsync(of => of.OrderStatusId == id);

            var response = new OrderStatusResponse
            {
                Id = orderStatus.Id,
                Name = orderStatus.Name,
                Description = orderStatus.Description,
                IsActive = orderStatus.IsActive,
                CreatedAt = orderStatus.CreatedAt,
                UpdatedAt = orderStatus.UpdatedAt,
                CreatedBy = orderStatus.CreatedBy,
                UpdatedBy = orderStatus.UpdatedBy,
                OrderCount = orderCount,
                OrderFlowCount = orderFlowCount
            };

            _logger.LogInformation("Order status updated successfully with ID: {OrderStatusId}", id);
            return Result<OrderStatusResponse>.Success(response, "Order status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status with ID: {OrderStatusId}", id);
            return Result<OrderStatusResponse>.Error("An error occurred while updating the order status.");
        }
    }

    public async Task<Result<OrderStatusResponse>> GetByIdAsync(int id)
    {
        try
        {
            var orderStatus = await _context.Set<OrderStatus>()
                .FirstOrDefaultAsync(os => os.Id == id);

            if (orderStatus == null)
            {
                _logger.LogWarning("Order status with ID {OrderStatusId} not found", id);
                return Result<OrderStatusResponse>.NotFound();
            }

            // Get counts
            var orderCount = await _context.Orders.CountAsync(o => o.OrderStatusId == id);
            var orderFlowCount = await _context.Set<OrderFlow>().CountAsync(of => of.OrderStatusId == id);

            var response = new OrderStatusResponse
            {
                Id = orderStatus.Id,
                Name = orderStatus.Name,
                Description = orderStatus.Description,
                IsActive = orderStatus.IsActive,
                CreatedAt = orderStatus.CreatedAt,
                UpdatedAt = orderStatus.UpdatedAt,
                CreatedBy = orderStatus.CreatedBy,
                UpdatedBy = orderStatus.UpdatedBy,
                OrderCount = orderCount,
                OrderFlowCount = orderFlowCount
            };

            _logger.LogInformation("Retrieved order status with ID: {OrderStatusId}", id);
            return Result<OrderStatusResponse>.Success(response, "Order status retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order status with ID: {OrderStatusId}", id);
            return Result<OrderStatusResponse>.Error("An error occurred while retrieving the order status.");
        }
    }

    public async Task<Result<List<OrderStatusResponse>>> GetAllAsync()
    {
        try
        {
            var orderStatuses = await _context.Set<OrderStatus>()
                .OrderBy(os => os.Name)
                .ToListAsync();

            var responses = new List<OrderStatusResponse>();

            foreach (var orderStatus in orderStatuses)
            {
                // Get counts for each status
                var orderCount = await _context.Orders.CountAsync(o => o.OrderStatusId == orderStatus.Id);
                var orderFlowCount = await _context.Set<OrderFlow>().CountAsync(of => of.OrderStatusId == orderStatus.Id);

                responses.Add(new OrderStatusResponse
                {
                    Id = orderStatus.Id,
                    Name = orderStatus.Name,
                    Description = orderStatus.Description,
                    IsActive = orderStatus.IsActive,
                    CreatedAt = orderStatus.CreatedAt,
                    UpdatedAt = orderStatus.UpdatedAt,
                    CreatedBy = orderStatus.CreatedBy,
                    UpdatedBy = orderStatus.UpdatedBy,
                    OrderCount = orderCount,
                    OrderFlowCount = orderFlowCount
                });
            }

            _logger.LogInformation("Retrieved {Count} order statuses", responses.Count);
            return Result<List<OrderStatusResponse>>.Success(responses, $"Retrieved {responses.Count} order statuses successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order statuses");
            return Result<List<OrderStatusResponse>>.Error("An error occurred while retrieving order statuses.");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        try
        {
            var orderStatus = await _context.Set<OrderStatus>()
                .Include(os => os.Orders)
                .Include(os => os.OrderFlows)
                .FirstOrDefaultAsync(os => os.Id == id);

            if (orderStatus == null)
            {
                _logger.LogWarning("Order status with ID {OrderStatusId} not found for deletion", id);
                return Result<bool>.NotFound();
            }

            // Check if order status has associated orders
            if (orderStatus.Orders.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "OrderStatus",
                    ErrorMessage = "Cannot delete order status as it has associated orders. Please reassign orders first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Check if order status has associated order flows
            if (orderStatus.OrderFlows.Any())
            {
                var validationError = new ValidationError
                {
                    Key = "OrderStatus",
                    ErrorMessage = "Cannot delete order status as it has associated order flows. Please remove order flows first."
                };
                return Result<bool>.Invalid(new List<ValidationError> { validationError });
            }

            // Remove the order status
            _context.Set<OrderStatus>().Remove(orderStatus);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order status deleted successfully with ID: {OrderStatusId}", id);
            return Result<bool>.Success(true, "Order status deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order status with ID: {OrderStatusId}", id);
            return Result<bool>.Error("An error occurred while deleting the order status.");
        }
    }
}